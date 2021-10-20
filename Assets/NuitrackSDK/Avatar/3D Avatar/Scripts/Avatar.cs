using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JointType = nuitrack.JointType;

namespace NuitrackSDK.NuitrackDemos
{
    enum MappingMode
    {
        Indirect,
        Direct,
    }

    public class Avatar : MonoBehaviour
    {
        [Header("Body")]
        [SerializeField] Transform waist;
        [SerializeField] Transform torso;
        [SerializeField] Transform collar;
        [SerializeField] Transform neck;
        [SerializeField] Transform head;

        [Header("Left hand")]
        [SerializeField] Transform leftShoulder;
        [SerializeField] Transform leftElbow;
        [SerializeField] Transform leftWrist;

        [Header("Right hand")]
        [SerializeField] Transform rightShoulder;
        [SerializeField] Transform rightElbow;
        [SerializeField] Transform rightWrist;

        [Header("Left leg")]
        [SerializeField] Transform leftHip;
        [SerializeField] Transform leftKnee;
        [SerializeField] Transform leftAnkle;

        [Header("Right leg")]
        [SerializeField] Transform rightHip;
        [SerializeField] Transform rightKnee;
        [SerializeField] Transform rightAnkle;

        List<ModelJoint> modelJoints = new List<ModelJoint>();

        [Header ("Options")]
        [Tooltip ("(optional) Specify the transform, which represents the sensor " +
            "coordinate system, to display the Avatar in front of the sensor." +
            "\nCalibration is not supported." +
            "\n\nIf not specified, the object transformation is used.")]
        [SerializeField] Transform sensorSpace;
        [SerializeField] MappingMode mappingMode;
        [SerializeField] JointType rootJoint = JointType.Waist;

        [Header("VR settings")]
        [SerializeField] GameObject vrHead;
        [SerializeField] Transform headTransform;
        Transform spawnedHead;

        [Header("Calibration")]
        [SerializeField] bool recenterOnSuccess;
        TPoseCalibration tPoseCalibration;
        Vector3 basePivotOffset = Vector3.zero;
        Vector3 startPoint; //Root joint model bone position on start

        /// <summary> Model bones </summary> Dictionary with joints
        Dictionary<JointType, ModelJoint> jointsRigged = new Dictionary<JointType, ModelJoint>();

        void OnEnable()
        {
            tPoseCalibration = FindObjectOfType<TPoseCalibration>();
            tPoseCalibration.onSuccess += OnSuccessCalib;
        }

        void SetJoint(Transform tr, JointType jointType)
        {
            ModelJoint modelJoint = new ModelJoint()
            {
                bone = tr,
                jointType = jointType
            };

            modelJoints.Add(modelJoint);
        }

        bool IsTransformSpace
        {
            get
            {
                return sensorSpace == null || sensorSpace == transform;
            }
        }

        Transform SpaceTransform
        {
            get
            {
                return IsTransformSpace ? transform : sensorSpace;
            }
        }

        void Start()
        {
            SetJoint(waist, JointType.Waist);
            SetJoint(torso, JointType.Torso);
            SetJoint(collar, JointType.LeftCollar);
            SetJoint(collar, JointType.RightCollar);
            SetJoint(neck, JointType.Neck);
            SetJoint(head, JointType.Head);

            SetJoint(leftShoulder, JointType.LeftShoulder);
            SetJoint(leftElbow, JointType.LeftElbow);
            SetJoint(leftWrist, JointType.LeftWrist);

            SetJoint(rightShoulder, JointType.RightShoulder);
            SetJoint(rightElbow, JointType.RightElbow);
            SetJoint(rightWrist, JointType.RightWrist);

            SetJoint(leftHip, JointType.LeftHip);
            SetJoint(leftKnee, JointType.LeftKnee);
            SetJoint(leftAnkle, JointType.LeftAnkle);

            SetJoint(rightHip, JointType.RightHip);
            SetJoint(rightKnee, JointType.RightKnee);
            SetJoint(rightAnkle, JointType.RightAnkle);

            //Adding model bones and JointType keys
            //Adding rotation offsets of model bones and JointType keys

            //Iterate joints from the modelJoints array
            //base rotation of the model bone is recorded 
            //then the model bones and their jointType are added to the jointsRigged dictionary
            foreach (ModelJoint modelJoint in modelJoints)
            {
                if (transform == modelJoint.bone)
                    Debug.LogError("Base transform can't be bone!");

                if (modelJoint.bone)
                {
                    modelJoint.baseRotOffset = Quaternion.Inverse(SpaceTransform.rotation) * modelJoint.bone.rotation;
                    jointsRigged.Add(modelJoint.jointType.TryGetMirrored(), modelJoint);

                    //Adding base distances between the child bone and the parent bone 
                    if (modelJoint.jointType.GetParent() != JointType.None)
                        AddBoneScale(modelJoint.jointType.TryGetMirrored(), modelJoint.jointType.GetParent().TryGetMirrored());
                }
            }

            if (vrHead)
                spawnedHead = Instantiate(vrHead).transform;

            if (jointsRigged.ContainsKey(rootJoint))
            {
                Vector3 rootPosition = jointsRigged[rootJoint].bone.position;
                startPoint = SpaceTransform.InverseTransformPoint(rootPosition);
            }
        }

        /// <summary>
        /// Adding distance between the target and parent model bones
        /// </summary>
        void AddBoneScale(JointType targetJoint, JointType parentJoint)
        {
            //take the position of the model bone
            Vector3 targetBonePos = jointsRigged[targetJoint].bone.position;
            //take the position of the model parent bone  
            Vector3 parentBonePos = jointsRigged[parentJoint].bone.position;
            jointsRigged[targetJoint].baseDistanceToParent = Vector3.Distance(parentBonePos, targetBonePos);
            //record the Transform of the model parent bone
            jointsRigged[targetJoint].parentBone = jointsRigged[parentJoint].bone;
        }

        void Update()
        {
            //If a skeleton is detected, process the model
            if (CurrentUserTracker.CurrentSkeleton != null) 
                ProcessSkeleton(CurrentUserTracker.CurrentSkeleton);

            if (spawnedHead)
                spawnedHead.position = headTransform.position;
        }

        /// <summary>
        /// Getting skeleton data from thr sensor and updating transforms of the model bones
        /// </summary>
        void ProcessSkeleton(nuitrack.Skeleton skeleton)
        {
            if (mappingMode == MappingMode.Indirect)
            {
                Vector3 rootPosition = SpaceTransform.rotation * skeleton.GetJoint(rootJoint).ToVector3() * 0.001f + basePivotOffset;
                jointsRigged[rootJoint].bone.position = SpaceTransform.TransformPoint(rootPosition);
            }

            foreach (var riggedJoint in jointsRigged)
            {
                //Get joint from the Nuitrack
                nuitrack.Joint joint = skeleton.GetJoint(riggedJoint.Key);
                if (joint.Confidence > 0.01f)
                {
                    //Get modelJoint
                    ModelJoint modelJoint = riggedJoint.Value;

                    //Bone rotation
                    Quaternion jointRotation = IsTransformSpace ? joint.ToQuaternionMirrored() : joint.ToQuaternion();

                    modelJoint.bone.rotation = SpaceTransform.rotation * (jointRotation * modelJoint.baseRotOffset);

                    if (mappingMode == MappingMode.Direct)
                    {
                        Vector3 jointPos = (0.001f * joint.ToVector3()) + basePivotOffset;
                        Vector3 localPos = IsTransformSpace ? Quaternion.Euler(0, 180, 0) * jointPos : jointPos;

                        Vector3 newPos = SpaceTransform.TransformPoint(localPos);

                        modelJoint.bone.position = newPos;

                        //Bone scale
                        if (modelJoint.parentBone != null && modelJoint.jointType.GetParent() != rootJoint)
                        {
                            //Take the Transform of a parent bone
                            Transform parentBone = modelJoint.parentBone;
                            //calculate how many times the distance between the child bone and its parent bone has changed compared to the base distance (which was recorded at the start)
                            float scaleDif = modelJoint.baseDistanceToParent / Vector3.Distance(newPos, parentBone.position);
                            //change the size of the bone to the resulting value (On default bone size (1,1,1))
                            parentBone.localScale = Vector3.one / scaleDif;
                            //compensation for size due to hierarchy
                            parentBone.localScale *= parentBone.localScale.x / parentBone.lossyScale.x;
                        }
                    }
                }
            }
        }

        private void OnSuccessCalib(Quaternion rotation)
        {
            StartCoroutine(CalculateOffset());
        }

        public IEnumerator CalculateOffset()
        {
            if (!recenterOnSuccess || !IsTransformSpace) 
                yield break;
            
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (jointsRigged.ContainsKey(rootJoint))
            {
                Vector3 rootPosition = jointsRigged[rootJoint].bone.position;

                Vector3 rootSpacePosition = SpaceTransform.InverseTransformPoint(rootPosition);
                Vector3 newPivotOffset = startPoint - rootSpacePosition + basePivotOffset;
                newPivotOffset.x = 0;

                basePivotOffset = newPivotOffset;
            }
        }

        void OnDisable()
        {
            tPoseCalibration.onSuccess -= OnSuccessCalib;
        }
    }
}