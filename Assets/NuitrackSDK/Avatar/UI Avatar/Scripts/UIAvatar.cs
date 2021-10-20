using System.Collections.Generic;
using UnityEngine;

using System;

public class UIAvatar : MonoBehaviour
{
    [Header("Skeleton")]
    public bool autoProcessing = true;
    [SerializeField] GameObject jointPrefab = null, connectionPrefab = null;
    RectTransform parentRect;

    Dictionary<nuitrack.JointType, RectTransform> connections;
    Dictionary<nuitrack.JointType, RectTransform> joints;

    bool initialized = false;

    void Start()
    {
        if (!initialized)
            Init();
    }

    void Init()
    {
        CreateSkeletonParts();
        parentRect = GetComponent<RectTransform>();
        initialized = true;
    }

    void CreateSkeletonParts()
    {
        joints = new Dictionary<nuitrack.JointType, RectTransform>();
        connections = new Dictionary<nuitrack.JointType, RectTransform>();

        foreach (nuitrack.JointType jointType in Enum.GetValues(typeof(nuitrack.JointType)))
        {
            if (jointType == nuitrack.JointType.None)
                continue;

            if (jointPrefab != null)
            {
                GameObject joint = Instantiate(jointPrefab, transform);
                joint.SetActive(false);

                RectTransform jointRectTransform = joint.GetComponent<RectTransform>();
                joints.Add(jointType, jointRectTransform);
            }

            if (connectionPrefab != null)
            {
                GameObject connection = Instantiate(connectionPrefab, transform);
                connection.SetActive(false);

                RectTransform connectionRectTransform = connection.GetComponent<RectTransform>();
                connections.Add(jointType, connectionRectTransform);
            }
        }
    }

    void Update()
    {
        if (autoProcessing)
            ProcessSkeleton(CurrentUserTracker.CurrentSkeleton);
    }

    public void ProcessSkeleton(nuitrack.Skeleton skeleton)
    {
        if (skeleton == null)
            return;

        if (!initialized)
            Init();

        foreach (KeyValuePair<nuitrack.JointType, RectTransform> jointsInfo in joints)
        {
            nuitrack.JointType jointType = jointsInfo.Key;
            RectTransform rectTransform = jointsInfo.Value;

            nuitrack.Joint j = skeleton.GetJoint(jointType);
            if (j.Confidence > 0.01f)
            {
                rectTransform.gameObject.SetActive(true);

                Vector2 newPosition = new Vector2(
                    parentRect.rect.width * (Mathf.Clamp01(j.Proj.X) - 0.5f),
                    parentRect.rect.height * (0.5f - Mathf.Clamp01(j.Proj.Y)));

                rectTransform.anchoredPosition = newPosition;
            }
            else
            {
                rectTransform.gameObject.SetActive(false);
            }

            if(jointType.GetParent() != nuitrack.JointType.None)
            {
                RectTransform endJoint = joints[jointType.GetParent()];

                if (rectTransform.gameObject.activeSelf && endJoint.gameObject.activeSelf)
                {
                    connections[jointType].gameObject.SetActive(true);

                    connections[jointType].anchoredPosition = rectTransform.anchoredPosition;
                    connections[jointType].transform.right = endJoint.position - rectTransform.position;
                    float distance = Vector3.Distance(endJoint.anchoredPosition, rectTransform.anchoredPosition);
                    connections[jointType].transform.localScale = new Vector3(distance, 1f, 1f);
                }
                else
                {
                    connections[jointType].gameObject.SetActive(false);
                }
            }
        }
    }
}
