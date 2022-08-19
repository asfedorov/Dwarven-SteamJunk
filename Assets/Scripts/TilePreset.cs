using System;
using System.Collections.Generic;
using UnityEngine;
using  UnityEngine.Tilemaps;

namespace WPTest.Scripts
{
    public enum TileNeighbours
    {
        Left,
        LeftUp,
        Up,
        UpRight,
        Right,
        RightDown,
        Down,
        DownLeft
    }

    public enum TileDirection
    {
        Left,
        Up,
        Right,
        Down
    }

    public enum TileVerticalDirection
    {
        Top,
        Bottom
    }

    [CreateAssetMenu(fileName = "TilePreset", menuName = "Tile Preset", order = 0)]
    public class TilePreset : ScriptableObject
    {
        // public GameObject Prefab;
        public Tile Prefab;
        public TileDirection Direction;
        public Vector3 Rotation;

        public int level = 1;
        public int probability = 1;


        public TileDirection[] PossibleDirections = new []
        {
            TileDirection.Left,
            TileDirection.Up,
            TileDirection.Right,
            TileDirection.Down
        };
        public string[] Connections = new string [4];
        public string[] VerticalConnections = new string[2];

        public Vector3 GetObjectRotationByDirection(TileDirection direction) {
            return Rotation + new Vector3 (0f, (int)direction * 90f, 0f);
        }

        public string[] GetConnectionsByDirection(TileDirection direction)
        {
            // Debug.Log($"Getting connections on {direction} direction");
            string[] newConnections = new string[4];

            for (int i =0; i < 4; i++)
            {
                int newIndex = i + (int)direction;
                if (newIndex >= 4)
                {
                    newIndex -= 4;
                }
                newConnections[newIndex] = Connections[i];
                // newConnections[i] = Connections[newIndex];
            }

            return newConnections;
        }

        public string[] GetConnections()
        {
            return Connections;
        }

        public string[] GetVerticalConnections()
        {
            return VerticalConnections;
        }

        public string[] GetVerticalConnectionsByDirection(TileDirection direction)
        {
            string[] newConnections = new string[2];

            for (int i = 0; i < 2; i++)
            {
                var i0 = 0 + (int)direction;
                var i1 = 1 + (int)direction;
                var i2 = 2 + (int)direction;
                var i3 = 3 + (int)direction;

                if (i0 >= 4) { i0 -= 4; }
                if (i1 >= 4) { i1 -= 4; }
                if (i2 >= 4) { i2 -= 4; }
                if (i3 >= 4) { i3 -= 4; }

                newConnections[i] = $"{VerticalConnections[i][i0]}{VerticalConnections[i][i1]}{VerticalConnections[i][i2]}{VerticalConnections[i][i3]}";
            }

            return newConnections;
        }

        public bool CanConnectByDirection(string connection, TileDirection direction, TileDirection rotation)
        {
            string invertedConnection = $"{connection[1]}{connection[0]}{connection[3]}{connection[2]}";
            string checkConnection = GetConnectionsByDirection(rotation)[(int)direction];



            // bool result = invertedConnection == checkConnection;
            // bool result = invertedConnection[0] == checkConnection[0] && invertedConnection[1] == checkConnection[1] && invertedConnection[2] == checkConnection[2];
            // if (checkConnection == "0000")
            // {
            //     return true;
            // }
            // Debug.Log($"Compare {connection} | {invertedConnection} with {GetConnectionsByDirection(rotation)[(int)direction]}; Result: {result}");
            return CheckEquality(invertedConnection, checkConnection);
        }

        public bool CanConnectByDirection(string connection, TileDirection direction)
        {
            string invertedConnection = $"{connection[2]}{connection[1]}{connection[0]}";
            // string invertedConnection = $"{connection[1]}{connection[0]}{connection[3]}{connection[2]}";
            string checkConnection = GetConnections()[(int)direction];



            // bool result = invertedConnection == checkConnection;
            // bool result = invertedConnection[0] == checkConnection[0] && invertedConnection[1] == checkConnection[1] && invertedConnection[2] == checkConnection[2];
            // if (checkConnection == "0000")
            // {
            //     return true;
            // }
            // Debug.Log($"Compare {connection} | {invertedConnection} with {GetConnectionsByDirection(rotation)[(int)direction]}; Result: {result}");
            return CheckEquality(invertedConnection, checkConnection);
        }

        public bool CanConnectByVerticalDirection(string connection, TileVerticalDirection direction, TileDirection rotation)
        {
            var checkConnection = GetVerticalConnectionsByDirection(rotation)[(int)direction];

            // if (checkConnection == "0000")
            // {
            //     return true;
            // }
            // bool result = connection == GetVerticalConnectionsByDirection(rotation)[(int)direction];
            // Debug.Log($"Compare {connection} with {GetVerticalConnectionsByDirection(rotation)[(int)direction]}; Result: {result}");

            return CheckEquality(checkConnection, connection);
        }

        public bool CanConnectByVerticalDirection(string connection, TileVerticalDirection direction)
        {
            var checkConnection = GetVerticalConnections()[(int)direction];

            // if (checkConnection == "0000")
            // {
            //     return true;
            // }
            // bool result = connection == GetVerticalConnectionsByDirection(rotation)[(int)direction];
            // Debug.Log($"Compare {connection} with {GetVerticalConnectionsByDirection(rotation)[(int)direction]}; Result: {result}");

            return CheckEquality(checkConnection, connection);
        }

        bool CheckEquality(string str1, string str2)
        {
            for (int i = 0; i < str1.Length; i++)
            {
                if (str1[i] == 'x')
                {
                    continue;
                }

                if (str2[i] == 'x')
                {
                    continue;
                }

                if (str1[i] != str2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }

}
