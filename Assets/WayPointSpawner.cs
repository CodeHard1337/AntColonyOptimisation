using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPointSpawner : MonoBehaviour
{
    [SerializeField] private Vector2 m_BottomLeft;
    [SerializeField] private Vector2 m_TopRight;
    [SerializeField] private int m_Amount;

    [SerializeField] private GameObject m_WayPointPrefab;
    [SerializeField] private Transform m_WayPointsParent;

    List<Transform> m_WayPoints = new List<Transform>();

    public void SpawnWayPoints()
    {
        foreach (var wp in m_WayPoints)
        {
            Destroy(wp.gameObject);
        }
        m_WayPoints.Clear();

        for (int i = 0; i < m_Amount; i++)
        {
            float x = Random.Range(m_BottomLeft.x, m_TopRight.x);
            float y = Random.Range(m_BottomLeft.y, m_TopRight.y);
            Vector2 pos = new Vector2(x, y);

            GameObject wp = Instantiate(m_WayPointPrefab, pos, Quaternion.identity, m_WayPointsParent);
            m_WayPoints.Add(wp.transform);
        }

        FindObjectOfType<RouteCalculator>().SetWayPoints(m_WayPoints);
    }
}
