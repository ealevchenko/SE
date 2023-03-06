using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using VRage.Game;
using VRage.Library;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Common;
using Sandbox.Game;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;

namespace gorizont
{
    public sealed class Program : MyGridProgram
    {
        // Название 
        string NameGroup = "КРОТИК1";
        string NameCockpit = "Промышленный кокпит 1";

        List<IMyTerminalBlock> list_block = new List<IMyTerminalBlock>();                  // Список всех блоков
        IMyShipController cockpit;
        List<IMyGyro> gyrolist = new List<IMyGyro>();
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            // Поиск объектов
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(list_block, r => r.CustomName.Contains(NameGroup));
            foreach (IMyTerminalBlock obj in list_block)
            {
                if (obj.CustomName.Contains(NameCockpit)) { cockpit = (IMyShipController)obj; }
                if (obj is IMyGyro) { gyrolist.Add((IMyGyro)obj); }                    // добавим все гироскопы
            }
            // Проверка объектов
            Echo("cockpit: " + ((cockpit != null) ? ("Ок") : ("not found")));
            Echo("gyrolist: " + ((gyrolist != null && gyrolist.Count()>0) ? ("Ок") : ("not gyrolist")));
        }

        public void Save()
        {

        }
        public void KeepHorizon()
        {
            Vector3D grav = Vector3D.Normalize(cockpit.GetNaturalGravity());
            Vector3D axis = grav.Cross(cockpit.WorldMatrix.Down);
            if (grav.Dot(cockpit.WorldMatrix.Down) < 0)
            {
                axis = Vector3D.Normalize(axis);
            }
            SetGyro(axis);
        }
        public void SetGyro(Vector3D axis)
        {
            foreach (IMyGyro gyro in gyrolist)
            {
                gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up);
                gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right);
                gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward);
            }
        }
        public void GyroOver(bool over)
        {
            foreach (IMyGyro gyro in gyrolist)
            {
                gyro.Yaw = 0;
                gyro.Pitch = 0;
                gyro.Roll = 0;
                gyro.GyroOverride = over;
            }
        }
        public void Main(string arg, UpdateType uType)
        {
            if (uType == UpdateType.Update1)
            {
                KeepHorizon();
            }
            else
            {
                switch (arg)
                {
                    case "horizont_on":
                        GyroOver(true);
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        break;
                    case "horizont_off":
                        GyroOver(false);
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        break;
                    default:
                        break;
                }
            }
        }

    }
}
