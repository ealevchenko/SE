using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRageMath;

namespace SANA1_NAVIGATION
{
    public sealed class Program : MyGridProgram
    {
        // Название 
        string NameObj = "SANA1";
        string NameCockpit = "SANA1-Кресло пилота [LCD]";
        string NameCamera = "SANA1-камера forward";
        string NameRemoteControl = "SANA1-ДУ forward";
        string NameLCD = "SANA1-LCD навигация";

        IMyTextPanel test_lcd_navigation;
        Cockpit cockpit;
        RemoteControl rem_con;
        CameraBlock camera;
        Gyro gyrs;

        Vector3D target;
        bool enable_drill = false;          // Бит режим сверлим дыру
        bool enable_navigation = false;     // Бит летим к цели
        static Program _scr;

        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public BaseTerminalBlock(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T;
                _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public BaseTerminalBlock(T myobj)
            {
                obj = myobj;
                _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            // Команды включения\выключения
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
        }

        public class BaseListTerminalBlock<T> where T : class
        {
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo(typeof(T).Name + "[" + name_obj + "]" + ((list_obj != null && list_obj.Count() > 0) ? ("Ок") : ("not found"))); ;
            }
            // Команды включения\выключения
            private void Off(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_Off");
                }
            }
            public void Off()
            {
                Off(list_obj);
            }
            private void OffOfTag(List<T> list, string tag)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(tag))
                    {
                        obj.ApplyAction("OnOff_Off");
                    }
                }
            }
            public void OffOfTag(string tag)
            {
                OffOfTag(list_obj, tag);
            }
            private void On(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_On");
                }
            }
            public void On()
            {
                On(list_obj);
            }
            private void OnOfTag(List<T> list, string tag)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(tag))
                    {
                        obj.ApplyAction("OnOff_On");
                    }
                }
            }
            public void OnOfTag(string tag)
            {
                OnOfTag(list_obj, tag);
            }
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            test_lcd_navigation = GridTerminalSystem.GetBlockWithName(NameLCD) as IMyTextPanel;
            Echo("NameLCD: " + ((test_lcd_navigation != null) ? ("Ок") : ("not found")));

            cockpit = new Cockpit(NameCockpit);
            rem_con = new RemoteControl(NameRemoteControl);
            camera = new CameraBlock(NameCamera);
            gyrs = new Gyro(NameObj);
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            string message = "";
            switch (argument)
            {
                case "target":
                    target = camera.GetVectorForward();
                    break;
                case "start_drill":
                    if (!enable_navigation)
                    {
                        gyrs.GyroOver(true);
                        enable_drill = true;
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    }
                    else {
                        message = "Включен режим - АВТОПОЛЕТ!";
                    }
                    break;
                case "stop_drill":
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    gyrs.GyroOver(false);
                    enable_drill = false;
                    break;
                case "start_move":
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    if (!enable_drill)
                    {
                        //gyrs.GyroOver(true);
                        //enable_drill = true;
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    }
                    else
                    {
                        message = "Включен режим - БУРЕНИЕ!";
                    }
                    break;
                case "stop_move":
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                // Держать направление
                if (enable_drill || enable_navigation)
                {
                    KeepAxis();
                }
            }
            values_info.Append("Цель: " + target.ToString() + "\n");
            values_info.Append("Растояние: " + rem_con.GetDistance(target).ToString() + "\n");
            values_info.Append("Режим БУРЕНИЕ: " + (enable_drill ? "Вкл" : "Вык.") + "\n");
            values_info.Append("Режим АВТОПОЛЕТ: " + (enable_navigation ? "Вкл" : "Вык.") + "\n");
            values_info.Append("Info: " + message + "\n");
            if (test_lcd_navigation != null)
            {
                test_lcd_navigation.WriteText(values_info, false);
            }
        }

        public void KeepAxis()
        {
            gyrs.SetGyro(Vector3D.Normalize(target));
        }

        public class Cockpit : BaseTerminalBlock<IMyShipController>
        {
            public bool IsUnderControl { get { return obj.IsUnderControl; } }
            public Cockpit(string name) : base(name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController;
                _scr.Echo("cockpit: " + ((obj != null) ? ("Ок") : ("not found")));
            }
            public void Dampeners(bool on)
            {
                obj.DampenersOverride = on;
            }
            // Получить axis горизонта
            public Vector3D GetAxisHorizon()
            {
                Vector3D grav = Vector3D.Normalize(obj.GetNaturalGravity());
                Vector3D axis = grav.Cross(obj.WorldMatrix.Down);
                if (grav.Dot(obj.WorldMatrix.Down) < 0)
                {
                    axis = Vector3D.Normalize(axis);
                }
                return axis;
            }
        }
        public class RemoteControl : BaseTerminalBlock<IMyRemoteControl>
        {
            public bool IsUnderControl { get { return obj.IsUnderControl; } }
            public RemoteControl(string name) : base(name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyRemoteControl;
                _scr.Echo("cockpit: " + ((obj != null) ? ("Ок") : ("not found")));
            }
            public void Dampeners(bool on)
            {
                obj.DampenersOverride = on;
            }

            public double GetDistance(Vector3D target)
            {
                return (target - obj.GetPosition()).Length();
            }

        }
        public class CameraBlock : BaseTerminalBlock<IMyCameraBlock>
        {
            public CameraBlock(string name) : base(name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyCameraBlock;
                _scr.Echo("cockpit: " + ((obj != null) ? ("Ок") : ("not found")));
            }
            public Vector3D GetVectorForward()
            {
                return obj.WorldMatrix.Forward;
                //return Vector3D.Normalize(obj.WorldMatrix.Forward);
            }
        }
        public class Gyro : BaseListTerminalBlock<IMyGyro>
        {
            public Gyro(string name_obj) : base(name_obj)
            {

            }
            public void SetGyro(Vector3D axis)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up);
                    gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right);
                    gyro.Roll = 0;// (float)axis.Dot(gyro.WorldMatrix.Backward);
                }
            }
            public void GyroOver(bool over)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = over;
                }
            }
        }
    }
}
