using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RouteCalculator : MonoBehaviour
{
    struct Ant
    {
        public List<int> visitedWayPointIndices;
        public int currentIndex;
    }

    struct WaypointProbability
    {
        public int index;
        public float probability;
    }

    [Header("[AMOUNTS]")]
    [SerializeField] private int m_AntsPerIteration = 5;
    [SerializeField] private int m_Iterations = 20;

    [Header("[POWERS]")]
    [SerializeField] private float m_DistancePower = 1;
    [SerializeField] private float m_PheremonePower = 1;

    [Header("[PHEREMONES]")]
    [SerializeField] private float m_StartingPheremones = 1;
    [SerializeField] private float m_Evaporation = 0.5f;
    [SerializeField] private float m_PheremoneIntensity = 10f;

    [Header("[OPTIMISATION]")]
    [SerializeField] private bool m_OptimisePathAfterCalculation = true;
    [SerializeField] private int m_OptimisationIterations = 2;

    [Header("[WAYPOINTS]")]
    [SerializeField] private List<Transform> m_WayPoints = new List<Transform>();

    [Header("[VISUAL]")]
    [SerializeField] private Material m_LineMaterial;
    [SerializeField] private float m_TimeBetweenIterations;
    [SerializeField] private Transform m_LinesParent;

    [Header("[UI]")]
    [SerializeField] private Text m_BestPathLengthText;
    [SerializeField] private Text m_IterationText;




    private float[,] m_Pheremones;

    List<int> m_BestPathIndices = new List<int>();
    float m_BestPathLength = float.PositiveInfinity;

    private List<GameObject> m_Lines = new List<GameObject>();




    public void SetWayPoints(List<Transform> wayPoints)
    {
        m_WayPoints = wayPoints;
    }
    
    public void StartComputing()
    {
        // Initialize
        m_BestPathLength = float.PositiveInfinity;
        InitPheremones();

        StopAllCoroutines();
        StartCoroutine(Compute());
    }

    IEnumerator Compute()
    {
        for (int iteration = 0; iteration < m_Iterations; iteration++)
        {
            m_IterationText.text = "Iteration - " + (iteration + 1).ToString();

            // Init pheremones to add
            float[,] pheremonesToAdd = new float[m_WayPoints.Count, m_WayPoints.Count];
            for (int i = 0; i < m_WayPoints.Count; i++)
            {
                for (int j = 0; j < m_WayPoints.Count; j++)
                {
                    pheremonesToAdd[i, j] = 0;
                }
            }

            // Run ants simulation
            for (int a = 0; a < m_AntsPerIteration; a++)
            {
                int pathStart = Random.Range(0, m_WayPoints.Count);

                Ant ant = new Ant();
                ant.currentIndex = pathStart;
                ant.visitedWayPointIndices = new List<int>();
                ant.visitedWayPointIndices.Add(pathStart);

                for (int c = 0; c < m_WayPoints.Count - 1; c++)
                {
                    int nextIndex = ChooseNextIndex(ant);

                    ant.visitedWayPointIndices.Add(nextIndex);
                    ant.currentIndex = nextIndex;
                }

                // Close loop
                ant.visitedWayPointIndices.Add(ant.visitedWayPointIndices[0]);

                LeavePheremones(ant, ref pheremonesToAdd);

                UpdateBestPath(ant);
            }

            // Update pheremones
            UpdatePheremones(pheremonesToAdd);

            DrawPath(m_BestPathIndices, new Color(1, 1, 1));
            yield return new WaitForSeconds(m_TimeBetweenIterations);
        }

        OptimisePath(m_BestPathIndices);
        DrawPath(m_BestPathIndices, new Color(1, 1, 1));
    }

    // PATH
    //------

    float PathLength(List<int> path)
    {
        float length = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            int idx1 = path[i];
            int idx2 = path[i + 1];

            length += Vector2.Distance(m_WayPoints[idx1].position, m_WayPoints[idx2].position);
        }

        return length;
    }

    void OptimisePath(List<int> path)
    {
        // Code partially from https://www.technical-recipes.com/2017/applying-the-2-opt-algorithm-to-travelling-salesman-problems-in-c-wpf/

        path.Remove(path.Count - 1);
        var size = path.Count;

        List<int> newPath = new List<int>();  
        foreach (var wp in path)
        {
            newPath.Add(wp);
        }

        for (int it = 0; it < m_OptimisationIterations; it++)
        {
            for (int i = 1; i < size - 3; i++)
            {
                for (int j = i + 1; j < size - 1; j++)
                {
                    float L_i_i1  = Vector2.Distance(m_WayPoints[newPath[i]].position,     m_WayPoints[newPath[i + 1]].position);
                    float L_j_j1  = Vector2.Distance(m_WayPoints[newPath[j]].position,     m_WayPoints[newPath[j + 1]].position);
                    float L_i_j   = Vector2.Distance(m_WayPoints[newPath[i]].position,     m_WayPoints[newPath[j]].position);
                    float L_i1_j1 = Vector2.Distance(m_WayPoints[newPath[i + 1]].position, m_WayPoints[newPath[j + 1]].position);

                    if (L_i_i1 + L_j_j1 > L_i_j + L_i1_j1)
                    {
                        for (int k = 0; k < (j-i)/2; ++k)
                        {
                            int temp = newPath[j - k];
                            newPath[j - k] = newPath[i + k + 1];
                            newPath[i + k + 1] = temp;
                        }

                        path.Clear();
                        foreach (var wp in newPath)
                        {
                            path.Add(wp);
                        }
                    }
                }
            }
        }

        path.Add(path[0]);
    }

    void UpdateBestPath(Ant ant)
    {
        float length = PathLength(ant.visitedWayPointIndices);
        if (length < m_BestPathLength)
        {
            m_BestPathIndices = ant.visitedWayPointIndices;
            m_BestPathLength = length;

            m_BestPathLengthText.text = "Length - " + length.ToString();
        }
    }


    // PHEREMONES
    //------------

    void InitPheremones()
    {
        m_Pheremones = new float[m_WayPoints.Count, m_WayPoints.Count];
        for (int i = 0; i < m_WayPoints.Count; i++)
        {
            for (int j = 0; j < m_WayPoints.Count; j++)
            {
                m_Pheremones[i, j] = m_StartingPheremones;
            }
        }
    }

    void UpdatePheremones(float[,] pheremonesToAdd)
    {
        for (int i = 0; i < m_WayPoints.Count; i++)
        {
            for (int j = 0; j < m_WayPoints.Count; j++)
            {
                m_Pheremones[i, j] = m_Evaporation * m_Pheremones[i, j] + pheremonesToAdd[i, j];
            }
        }
    }

    void LeavePheremones(Ant ant, ref float[,] pheremonesToAdd)
    {
        float length = 0;
        for (int i = 0; i < ant.visitedWayPointIndices.Count - 1; i++)
        {
            int idx1 = ant.visitedWayPointIndices[i];
            int idx2 = ant.visitedWayPointIndices[i + 1];

            length += Vector2.Distance(m_WayPoints[idx1].position, m_WayPoints[idx2].position);
        }

        float pheremoneValue = m_PheremoneIntensity / length;

        for (int i = 0; i < ant.visitedWayPointIndices.Count - 1; i++)
        {
            int idx1 = ant.visitedWayPointIndices[i];
            int idx2 = ant.visitedWayPointIndices[i + 1];

            pheremonesToAdd[idx1, idx2] += pheremoneValue;
            pheremonesToAdd[idx2, idx1] += pheremoneValue;
        }
    }


    // PROBABILITIES
    //---------------

    int ChooseNextIndex(Ant ant)
    {
        // Desirability list
        List<WaypointProbability> desirabilities = new List<WaypointProbability>();
        float totalDesirability = 0;

        for (int i = 0; i < m_WayPoints.Count; i++)
        {
            if (!ant.visitedWayPointIndices.Contains(i))
            {
                Vector2 currentPos = m_WayPoints[ant.currentIndex].position;
                float distance = Vector2.Distance(currentPos, m_WayPoints[i].position);
                float pheremones = m_Pheremones[ant.currentIndex, i];

                float desirability = Mathf.Pow(1 / distance, m_DistancePower) * Mathf.Pow(pheremones, m_PheremonePower);
                
                WaypointProbability wpp = new WaypointProbability();
                wpp.index = i;
                wpp.probability = desirability;
                desirabilities.Add(wpp);

                totalDesirability += desirability;
            }
        }

        // Probability list
        List<WaypointProbability> probabilities = new List<WaypointProbability>();

        for (int i = 0; i < desirabilities.Count; i++)
        {
            if (totalDesirability != 0)
            {
                WaypointProbability wpp = new WaypointProbability();
                wpp.index = desirabilities[i].index;
                wpp.probability = desirabilities[i].probability / totalDesirability;

                probabilities.Add(wpp);
            }
            else
            {
                int nrNotVisited = m_WayPoints.Count - ant.visitedWayPointIndices.Count;

                WaypointProbability wpp = new WaypointProbability();
                wpp.index = desirabilities[i].index;
                wpp.probability = 1 / nrNotVisited;

                probabilities.Add(wpp);
            }
        }

        // Cumulative probability list
        List<WaypointProbability> cumulativeProbabilities = new List<WaypointProbability>();
        float cumulative = 0;

        for (int i = 0; i < probabilities.Count - 1; i++)
        {
            cumulative += probabilities[i].probability;

            WaypointProbability wpp = new WaypointProbability();
            wpp.index = probabilities[i].index;
            wpp.probability = cumulative;

            cumulativeProbabilities.Add(wpp);
        }
        cumulativeProbabilities.Add(new WaypointProbability {
            index = probabilities[probabilities.Count - 1].index,
            probability = 1 });

        float random = Random.Range(0.0f, 1.0f);
        for (int i = 0; i < cumulativeProbabilities.Count; i++)
        {
            if (random <= cumulativeProbabilities[i].probability)
                return cumulativeProbabilities[i].index;
        }

        return -1;
    }


    // DRAW FUNCTIONS
    //----------------

    void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        // Line render code partially from https://answers.unity.com/questions/8338/how-to-draw-a-line-using-script.html

        GameObject myLine = new GameObject();
        myLine.transform.SetParent(m_LinesParent, true);
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();

        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = m_LineMaterial;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        m_Lines.Add(myLine);
    }

    void DrawPath(List<int> pathIndices, Color color)
    {
        foreach (var l in m_Lines)
        {
            Destroy(l);
        }
        m_Lines.Clear();

        for (int i = 0; i < pathIndices.Count - 1; i++)
        {
            int startIdx = pathIndices[i];
            int endIdx = pathIndices[i + 1];

            DrawLine(m_WayPoints[startIdx].position, m_WayPoints[endIdx].position, color);
        }
    }
}