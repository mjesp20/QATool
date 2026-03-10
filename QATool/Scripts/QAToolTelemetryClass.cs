using System;
using System.Collections.Generic;
using UnityEngine;

namespace QATool
{
    [Serializable]
    public class QAToolTelemetryClass
    {
        [Serializable]
        public class PlayerPosition
        {
            public float x;
            public float y;
            public float z;

            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
        }

        [Serializable]
        public class PlayerVelocity
        {
            public float x;
            public float y;
            public float z;

            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
        }
        public class PlayerCamera
        {
            public float x;
            public float y;
            public float z;

            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
        }

        [Serializable]
        public class Entry
        {
            public PlayerPosition PlayerPosition;
            public PlayerVelocity PlayerVelocity;
            public PlayerCamera PlayerCamera;
            public string type;
            public float time;
            public int playerID;
            public Dictionary<string, object> args;
        }
    }
}