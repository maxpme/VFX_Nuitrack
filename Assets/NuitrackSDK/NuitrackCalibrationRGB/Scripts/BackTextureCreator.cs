using UnityEngine;
using System.Collections.Generic;

using NuitrackSDK.Frame;

namespace NuitrackSDK.VicoVRCalibration
{
    public class BackTextureCreator : MonoBehaviour
    {
        [SerializeField] bool userColorizeEnable = false;
        Texture tex;
        Texture userTex;

        public Texture GetRGBTexture
        {
            get
            {
                return (Texture)tex;
            }
        }
        public Texture GetUserTexture
        {
            get
            {
                return (Texture)userTex;
            }
        }
        public delegate void newBackGroundCreate(Texture txtr, Texture userTxtr);
        static public event newBackGroundCreate newTextureEvent;

        Dictionary<ushort, Color> UsersColor;

        void Start()
        {
            UsersColor = new Dictionary<ushort, Color>();
            UsersColor.Add(0, new Color(0, 0, 0, 0));
            UsersColor.Add(1, Color.red);
            UsersColor.Add(2, Color.red);
            UsersColor.Add(3, Color.red);
            UsersColor.Add(4, Color.red);
            UsersColor.Add(5, Color.red);
        }

        void Update()
        {
            if (NuitrackManager.ColorFrame == null && NuitrackManager.DepthFrame == null)
                return;

            if (NuitrackManager.ColorFrame != null)
                tex = NuitrackManager.ColorFrame.ToRenderTexture();
            else
                tex = NuitrackManager.DepthFrame.ToRenderTexture();

            if (userColorizeEnable)
                userTex = NuitrackManager.UserFrame.ToRenderTexture();

            if (tex != null)
            {
                if (newTextureEvent != null)
                    newTextureEvent(tex, userTex);
            }
        }
    }
}
