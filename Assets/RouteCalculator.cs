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

    [SerializeField] private float m_DstPower;

    [SerializeField] private List<Transform> m_WayPoints = new List<Transform>();

    private List<Ant> m_Ants = new List<Ant>();

    public void Compute()
    {
        for (int i = 0; i < m_Iterations; i++)
        {
            for (int a = 0; a < m_AntsPerIteration; a++)
            {

            }
        }

        Ant ant = new Ant();
        ant.currentIndex = 0;
        ant.visitedWayPointIndices = new List<int>();
        ant.visitedWayPointIndices.Add(0);

        int nextWayPoint = RandomWayPoint(ant);
        Debug.Log(nextWayPoint);
        DrawLine(m_WayPoints[nextWayPoint].position, m_WayPoints[ant.currentIndex].position, Color.white);
    }

    int RandomWayPoint(Ant ant)
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

                float desirability = Mathf.Pow(1 / distance, m_DstPower);
                desirabilities.Add(desirability);

                totalDesirability += desirability;
            }
        }

        // Probability list
        List<float> cumulativeProbabilities = new List<float>();
        float cumulativeProbability = 0;

        for (int i = 0; i < desirabilities.Count; i++)
        {
            cumulativeProbability += desirabilities[i] / totalDesirability;
            cumulativeProbabilities.Add(cumulativeProbability);
        }

        // Choose random
        float randomNumber = Random.Range(0.0f, 1.0f);

        for (int i = 0; i <= cumulativeProbabilities.Count; i++)
        {
            if (randomNumber < cumulativeProbabilities[i])
            {
                return i;
            }
        }

        return -1;
    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.SetColors(color, color);
        lr.SetWidth(0.1f, 0.1f);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }

}