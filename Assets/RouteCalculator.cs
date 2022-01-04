using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouteCalculator : MonoBehaviour
{
    struct Ant
    {
        public List<int> visitedWayPointIndices;
        public int currentIndex;
    }

    [SerializeField] private int m_AntsPerIteration = 5;
    [SerializeField] private int m_Iterations = 20;

    [SerializeField] private float m_DistancePower = 1;
    [SerializeField] private float m_PheremonePower = 1;

    [SerializeField] private float m_Evaporation = 0.5f;
    [SerializeField] private float m_PheremoneIntensity = 10f;

    [SerializeField] private List<Transform> m_WayPoints = new List<Transform>();

    [SerializeField] private Material m_LineMaterial;
    [SerializeField] private float m_TimeBetweenIterations;

    private float[,] m_Pheremones;

    List<int> m_BestPathIndices = new List<int>();
    float m_BestPathLength = float.PositiveInfinity;

    public void StartComputing()
    {
        StartCoroutine(Compute());
    }

    IEnumerator Compute()
    {
        InitPheremones();

        for (int iteration = 0; iteration < m_Iterations; iteration++)
        {
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

                    List<float> probabilities = GetIndexProbabilities(ant);

                    int nextIndex = ChooseRandomFromProbabilities(probabilities);

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

            DrawPath(m_BestPathIndices, new Color(1, 1, 1), m_TimeBetweenIterations);
            yield return new WaitForSeconds(m_TimeBetweenIterations);
        }
    }

    void UpdateBestPath(Ant ant)
    {
        float length = 0;
        for (int i = 0; i < ant.visitedWayPointIndices.Count - 1; i++)
        {
            int idx1 = ant.visitedWayPointIndices[i];
            int idx2 = ant.visitedWayPointIndices[i + 1];

            length += Vector2.Distance(m_WayPoints[idx1].position, m_WayPoints[idx2].position);
        }

        if (length < m_BestPathLength)
        {
            m_BestPathIndices = ant.visitedWayPointIndices;
        }
    }

    void InitPheremones()
    {
        m_Pheremones = new float[m_WayPoints.Count, m_WayPoints.Count];
        for (int i = 0; i < m_WayPoints.Count; i++)
        {
            for (int j = 0; j < m_WayPoints.Count; j++)
            {
                m_Pheremones[i, j] = 0;
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

    List<float> GetIndexProbabilities(Ant ant)
    {
        // Desirability list
        List<float> desirabilities = new List<float>();
        float totalDesirability = 0;

        for (int i = 0; i < m_WayPoints.Count; i++)
        {
            if (!ant.visitedWayPointIndices.Contains(i))
            {
                Vector2 currentPos = m_WayPoints[ant.currentIndex].position;
                float distance = Vector2.Distance(currentPos, m_WayPoints[i].position);
                float pheremones = m_Pheremones[ant.currentIndex, i];

                float desirability = Mathf.Pow(1 / distance, m_DistancePower) * Mathf.Pow(pheremones, m_PheremonePower);
                desirabilities.Add(desirability);

                totalDesirability += desirability;
            }
            else
            {
                // already visited
                desirabilities.Add(0);
            }
        }

        // Probability list
        List<float> probabilities = new List<float>();

        for (int i = 0; i < desirabilities.Count; i++)
        {
            if (totalDesirability != 0)
            {
                probabilities.Add(desirabilities[i] / totalDesirability);
            }
            else
            {
                probabilities.Add(1.0f / desirabilities.Count);
            }
        }

        return probabilities;
    }

    int ChooseRandomFromProbabilities(List<float> probabilities)
    {
        List<float> cumulativeProbabilities = new List<float>();
        float cumulative = 0;

        for (int i = 0; i < probabilities.Count; i++)
        {
            cumulative += probabilities[i];
            cumulativeProbabilities.Add(cumulative);
        }

        float random = Random.Range(0.0f, 1.0f);
        for (int i = 0; i < cumulativeProbabilities.Count; i++)
        {
            if (random < cumulativeProbabilities[i])
                return i;
        }

        return -1;
    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
    {
        // Line render code partially from https://answers.unity.com/questions/8338/how-to-draw-a-line-using-script.html

        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = m_LineMaterial;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }

    void DrawProbabilities(List<float> probabilities, Vector2 startPos, float duration = 0.2f)
    {
        float maxProb = 0;
        foreach (float p in probabilities)
        {
            if (p > maxProb)
                maxProb = p;
        }

        for (int p = 0; p < probabilities.Count; p++)
        {
            Debug.Log(probabilities[p]);
            Color color = new Color(1.0f, 1.0f, 1.0f, probabilities[p] / maxProb);
            DrawLine(m_WayPoints[p].position, startPos, color, duration);
        }
    }

    void DrawPath(List<int> pathIndices, Color color, float duration = 0.2f)
    {
        for (int i = 0; i < pathIndices.Count - 1; i++)
        {
            int startIdx = pathIndices[i];
            int endIdx = pathIndices[i + 1];

            DrawLine(m_WayPoints[startIdx].position, m_WayPoints[endIdx].position, color, duration);
        }
    }

    void DrawPheremones(float duration)
    {
        for (int i = 0; i < m_WayPoints.Count; i++)
        {
            for (int j = 0; j < m_WayPoints.Count; j++)
            {
                DrawLine(m_WayPoints[i].position, m_WayPoints[j].position, new Color(1.0f, 1.0f, 1.0f, m_Pheremones[i, j]), duration);
            }
        }
    }
}