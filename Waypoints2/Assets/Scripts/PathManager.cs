using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public Transform[] Waypoints;
    public Color rayColor = Color.white;

    private void OnDrawGizmos()
    {
        Gizmos.color = rayColor;
        Transform[] path_nodes = transform.GetComponentsInChildren<Transform>();

        // Skip the parent (index 0)
        for (int i = 1; i < path_nodes.Length; i++)
        {
            Vector3 pos = path_nodes[i].position;

            // Draw line to previous node
            if (i > 1)
            {
                Vector3 prev = path_nodes[i - 1].position;
                Gizmos.DrawLine(prev, pos);
            }

            Gizmos.DrawSphere(pos, 1f);
        }
    }
}
