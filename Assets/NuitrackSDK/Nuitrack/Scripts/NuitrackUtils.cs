using UnityEngine;

using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using JointType = nuitrack.JointType;

public static class NuitrackUtils
{
    #region Transform
    public static Vector3 ToVector3(this nuitrack.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    public static Vector3 ToVector3(this nuitrack.Joint joint)
    {
        return new Vector3(joint.Real.X, joint.Real.Y, joint.Real.Z);
    }

    public static Quaternion ToQuaternion(this nuitrack.Joint joint)
    {
        Vector3 jointUp = new Vector3(joint.Orient.Matrix[1], joint.Orient.Matrix[4], joint.Orient.Matrix[7]);   //Y(Up)
        Vector3 jointForward = new Vector3(joint.Orient.Matrix[2], joint.Orient.Matrix[5], joint.Orient.Matrix[8]);   //Z(Forward)

        return Quaternion.LookRotation(jointForward, jointUp);
    }

    public static Quaternion ToQuaternionMirrored(this nuitrack.Joint joint)
    {
        Vector3 jointUp = new Vector3(-joint.Orient.Matrix[1], joint.Orient.Matrix[4], -joint.Orient.Matrix[7]);   //Y(Up)
        Vector3 jointForward = new Vector3(joint.Orient.Matrix[2], -joint.Orient.Matrix[5], joint.Orient.Matrix[8]);   //Z(Forward)

        if (jointForward.magnitude < 0.01f)
            return Quaternion.identity; //should not happen

        return Quaternion.LookRotation(jointForward, jointUp);
    }

    #endregion

    #region SkeletonUltils

    static readonly Dictionary<JointType, HumanBodyBones> nuitrackToUnity = new Dictionary<JointType, HumanBodyBones>()
    {
        {JointType.Head,                HumanBodyBones.Head},
        {JointType.Neck,                HumanBodyBones.Neck},
        {JointType.LeftCollar,          HumanBodyBones.LeftShoulder},
        {JointType.RightCollar,         HumanBodyBones.RightShoulder},
        {JointType.Torso,               HumanBodyBones.Hips},
        {JointType.Waist,               HumanBodyBones.Hips},   // temporarily


        {JointType.LeftFingertip,       HumanBodyBones.LeftMiddleDistal},
        {JointType.LeftHand,            HumanBodyBones.LeftMiddleProximal},
        {JointType.LeftWrist,           HumanBodyBones.LeftHand},
        {JointType.LeftElbow,           HumanBodyBones.LeftLowerArm},
        {JointType.LeftShoulder,        HumanBodyBones.LeftUpperArm},

        {JointType.RightFingertip,      HumanBodyBones.RightMiddleDistal},
        {JointType.RightHand,           HumanBodyBones.RightMiddleProximal},
        {JointType.RightWrist,          HumanBodyBones.RightHand},
        {JointType.RightElbow,          HumanBodyBones.RightLowerArm},
        {JointType.RightShoulder,       HumanBodyBones.RightUpperArm},


        {JointType.LeftFoot,            HumanBodyBones.LeftToes},
        {JointType.LeftAnkle,           HumanBodyBones.LeftFoot},
        {JointType.LeftKnee,            HumanBodyBones.LeftLowerLeg},
        {JointType.LeftHip,             HumanBodyBones.LeftUpperLeg},

        {JointType.RightFoot,           HumanBodyBones.RightToes},
        {JointType.RightAnkle,          HumanBodyBones.RightFoot},
        {JointType.RightKnee,           HumanBodyBones.RightLowerLeg},
        {JointType.RightHip,            HumanBodyBones.RightUpperLeg},
    };

    /// <summary>
    /// Returns the appropriate HumanBodyBones  for nuitrack.JointType
    /// </summary>
    /// <param name="nuitrackJoint">nuitrack.JointType</param>
    /// <returns>HumanBodyBones</returns>
    public static HumanBodyBones ToUnityBones(this JointType nuitrackJoint)
    {
        return nuitrackToUnity[nuitrackJoint];
    }

    static readonly Dictionary<JointType, JointType> mirroredJoints = new Dictionary<JointType, JointType>() {
        {JointType.LeftShoulder, JointType.RightShoulder},
        {JointType.RightShoulder, JointType.LeftShoulder},
        {JointType.LeftElbow, JointType.RightElbow},
        {JointType.RightElbow, JointType.LeftElbow},
        {JointType.LeftWrist, JointType.RightWrist},
        {JointType.RightWrist, JointType.LeftWrist},
        {JointType.LeftFingertip, JointType.RightFingertip},
        {JointType.RightFingertip, JointType.LeftFingertip},

        {JointType.LeftHip, JointType.RightHip},
        {JointType.RightHip, JointType.LeftHip},
        {JointType.LeftKnee, JointType.RightKnee},
        {JointType.RightKnee, JointType.LeftKnee},
        {JointType.LeftAnkle, JointType.RightAnkle},
        {JointType.RightAnkle, JointType.LeftAnkle},
    };

    public static JointType TryGetMirrored(this JointType joint)
    {
        JointType mirroredJoint = joint;
        if (NuitrackManager.DepthSensor.IsMirror() && mirroredJoints.ContainsKey(joint))
        {
            mirroredJoints.TryGetValue(joint, out mirroredJoint);
        }

        return mirroredJoint;
    }

    public static JointType GetParent(this JointType joint)
    {
        return parents[joint];
    }

    static Dictionary<JointType, JointType> parents = new Dictionary<JointType, JointType>()
    {
        {JointType.Waist,          JointType.None},
        {JointType.Torso,          JointType.Waist},
        {JointType.Neck,           JointType.LeftCollar},
        {JointType.Head,           JointType.Neck},
        {JointType.LeftCollar,     JointType.Torso},
        {JointType.RightCollar,    JointType.Torso},
        {JointType.LeftShoulder,   JointType.LeftCollar},
        {JointType.RightShoulder,  JointType.RightCollar},
        {JointType.LeftElbow,      JointType.LeftShoulder},
        {JointType.RightElbow,     JointType.RightShoulder},
        {JointType.LeftWrist,      JointType.LeftElbow},
        {JointType.RightWrist,     JointType.RightElbow},
        {JointType.LeftHand,       JointType.LeftWrist},
        {JointType.RightHand,      JointType.RightWrist},
        {JointType.LeftFingertip,  JointType.LeftHand},
        {JointType.RightFingertip, JointType.RightHand},
        {JointType.LeftHip,        JointType.Waist},
        {JointType.RightHip,       JointType.Waist},
        {JointType.LeftKnee,       JointType.LeftHip},
        {JointType.RightKnee,      JointType.RightHip},
        {JointType.LeftAnkle,      JointType.LeftKnee},
        {JointType.RightAnkle,     JointType.RightKnee},
        {JointType.LeftFoot,       JointType.LeftAnkle},
        {JointType.RightFoot,      JointType.RightAnkle},
    };

    static readonly List<JointType> sortedJoints = new List<JointType>()
    {
        // ------------------------------------------ 0 - the order (level) of the joint relative to none
        JointType.None,

        // ------------------------------------------ 1
        JointType.Waist,

        // ------------------------------------------ 2
        JointType.Torso,
        JointType.LeftHip,
        JointType.RightHip,

        // ------------------------------------------ 3
        JointType.Neck,
        JointType.LeftCollar,
        JointType.RightCollar,
        JointType.LeftKnee,
        JointType.RightKnee,

        // ------------------------------------------ 4
        JointType.Head,
        JointType.LeftShoulder,
        JointType.RightShoulder,
        JointType.LeftAnkle,
        JointType.RightAnkle,

        // ------------------------------------------ 5
        JointType.LeftElbow,
        JointType.RightElbow,
        JointType.LeftFoot,
        JointType.RightFoot,

        // ------------------------------------------ 6
        JointType.LeftWrist,
        JointType.RightWrist,

        // ------------------------------------------ 7
        JointType.LeftHand,
        JointType.RightHand,

        // ------------------------------------------ 8
        JointType.LeftFingertip,
        JointType.RightFingertip
    };

    /// <summary>
    /// Sorts the joints according to the hierarchy from the lowest joints (lower back) to the highest (wrists and ankles).
    /// Repetitions will be skipped.
    /// For the sorting order <see cref="sortedJoints"/> 
    /// </summary>
    /// <param name="sourceJoints">List of joints.</param>
    /// <returns>A sorted list of joints.</returns>
    public static List<JointType> SortClamp(this List<JointType> sourceJoints)
    {
        List<JointType> outList = new List<JointType>();

        foreach (JointType sortJoint in sortedJoints)
        {
            if (sourceJoints.Contains(sortJoint))
                outList.Add(sortJoint);
        }

        return outList;
    }

    #endregion

    #region JsonUtils

    static Regex regex = null;

    // A pattern for detecting any numbers, including exponential notation
    static string pattern = "\"-?[\\d]*\\.?[\\d]+(e[-+][\\d]+)?\"";

    public static T FromJson<T>(string json)
    {
        try
        {
            json = json.Replace("\"\"", "[]");

            if (regex == null)
                regex = new Regex(pattern);

            foreach (Match match in regex.Matches(json))
            {
                string withot_quotation_marks = match.Value.Replace("\"", "");
                json = json.Replace(match.Value, withot_quotation_marks);
            }

            T outData = JsonUtility.FromJson<T>(json);

            return outData;
        }
        catch (System.Exception e)
        {
            Debug.Log(string.Format("Json parsing failure\n{0}", e.Message));
            return default(T);
        }
    }

    #endregion
}