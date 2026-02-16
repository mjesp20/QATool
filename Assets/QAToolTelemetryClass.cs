using System;
using System.Collections.Generic;
using UnityEngine;

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
    public class Root
    {
        public string time;
        public string playerID;
        public PlayerPosition PlayerPosition;
    }
}
