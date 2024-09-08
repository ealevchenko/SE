using Newtonsoft.Json.Linq;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;

namespace IGC_Antena_Ship
{
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[SHIP-T]";

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static IMyTextPanel mesage_lcd;
        static IMyBroadcastListener edik;
        static IMyUnicastListener privat_chanel;
        static IMyProgrammableBlock pb;
        string tag = "chanel";
        static LCD lcd_dm;
        static LCD lcd_dm1;
        static MyIGCMessage message;
        static Connector connector_forw;
        static Cockpit cockpit;
        static Navigation nav;
        static MyStorage mystorage;
        static Program _scr;

        public class PText
        {
            static public string GetPersent(double perse) { return " - " + Math.Round((perse * 100), 1) + "%"; }
            static public string GetScalePersent(double perse, int scale) { string prog = "["; for (int i = 0; i < Math.Round((perse * scale), 0); i++) { prog += "|"; } for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++) { prog += "'"; } prog += "]" + GetPersent(perse); return prog; }
            static public string GetCurrentOfMax(float cur, float max, string units) { return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetCurrentOfMinMax(float min, float cur, float max, string units) { return "[ " + Math.Round(min, 1) + units + " / " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetThrust(float value) { return Math.Round(value / 1000000, 1) + "МН"; }
            static public string GetFarm(float value) { return Math.Round(value, 1) + "L"; }
            static public string GetGPS(string name, Vector3D target) { return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n"; }
            static public string GetGPSMatrixD(string name, MatrixD target) { return "MatrixD:" + name + "\n" + "M11:" + target.M11 + "M12:" + target.M12 + "M13:" + target.M13 + "M14:" + target.M14 + ":\n" + "M21:" + target.M21 + "M22:" + target.M22 + "M23:" + target.M23 + "M24:" + target.M24 + ":\n" + "M31:" + target.M31 + "M32:" + target.M32 + "M33:" + target.M33 + "M34:" + target.M34 + ":\n" + "M41:" + target.M41 + "M42:" + target.M42 + "M43:" + target.M43 + "M44:" + target.M44 + ":\n"; }
        }
        public class BaseListTerminalBlock<T> where T : class
        {
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "]: " + list_obj.Count());
            }
            public BaseListTerminalBlock(string name_obj, string tag)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "],[" + tag + "]: " + list_obj.Count());
            }
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
            public bool Enabled(string tag)
            {
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    if (obj.CustomName.Contains(tag) && !((IMyFunctionalBlock)obj).Enabled)
                    {
                        return false;
                    }
                }
                return true;
            }
            public bool Enabled()
            {
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    if (!((IMyFunctionalBlock)obj).Enabled)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public string CustomName { get { return ((IMyTerminalBlock)this.obj).CustomName; } set { ((IMyTerminalBlock)this.obj).CustomName = value; } }
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
            public Vector3D GetPosition()
            {
                return ((IMyEntity)obj).GetPosition();
            }
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
        }
        public class BaseController
        {
            public IMyShipController obj;
            private double current_height = 0;
            public double CurrentHeight { get { return this.current_height; } }
            public Matrix GetCockpitMatrix()
            {
                Matrix CockpitMatrix = new MatrixD();
                this.obj.Orientation.GetMatrix(out CockpitMatrix);
                return CockpitMatrix;
            }
            public BaseController(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController;
                _scr.Echo("base_controller:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public void Dampeners(bool on)
            {
                this.obj.DampenersOverride = on;
            }
            public void OutText(StringBuilder values, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(values, false);
                    }
                }
            }
            public void OutText(string text, bool append, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(text, append);
                    }
                }
            }
            public StringBuilder GetText(int num_lcd)
            {
                StringBuilder values = new StringBuilder();
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return null;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.ReadText(values);
                    }
                }
                return values;
            }
            public double GetCurrentHeight()
            {
                double cur_h = 0;
                this.obj.TryGetPlanetElevation(MyPlanetElevation.Surface, out cur_h);
                return cur_h;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Получить высоту над поверхностью
                    current_height = GetCurrentHeight();
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Гравитация: " + this.obj.GetNaturalGravity().Length() + "\n");
                values.Append("PhysicalMass: " + this.obj.CalculateShipMass().PhysicalMass + "\n");
                values.Append("Скорость: " + this.obj.GetShipSpeed() + "\n");
                values.Append("Высота: " + current_height + "\n");
                return values.ToString();
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            // тест LCD
            mesage_lcd = GridTerminalSystem.GetBlockWithName("mesage_lcd") as IMyTextPanel;
            Echo("test_lcd: " + ((mesage_lcd != null) ? ("Ок") : ("not found")));
            lcd_dm = new LCD(NameObj + "-debug message");
            lcd_dm1 = new LCD(NameObj + "-debug message1");
            edik = IGC.RegisterBroadcastListener(tag);
            privat_chanel = IGC.UnicastListener;
            pb = GridTerminalSystem.GetBlockWithName("[SHIP-T]-ПБ Antena") as IMyProgrammableBlock;

            message = new MyIGCMessage();
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD] Locked");
            connector_forw = new Connector(NameObj + "-Коннектор [forw]");
            nav = new Navigation();
            mystorage = new MyStorage();
        }

        void Main(string argument, UpdateType updateSource)
        {
            nav.Logic(argument, updateSource);
            if (argument == "test_privat")
            {
                IGC.SendBroadcastMessage<string>(tag, argument);//отправляю сообщение по нужному тегу
            }            
            if (argument == "conn_h1")
            {
                IGC.SendBroadcastMessage<string>(tag, argument);//отправляю сообщение по нужному тегу
            }
            if (argument == "conn_h2")
            {
                IGC.SendBroadcastMessage<string>(tag, argument);//отправляю сообщение по нужному тегу
            }
            if (argument == "conn_v1")
            {
                IGC.SendBroadcastMessage<string>(tag, argument);//отправляю сообщение по нужному тегу
            }
            if (argument == "conn_cp")
            {
                IGC.SendBroadcastMessage<string>(tag, argument);//отправляю сообщение по нужному тегу
            }

            if (privat_chanel.HasPendingMessage)
            {
                message = privat_chanel.AcceptMessage();
                mesage_lcd.WriteText("Unicast");
                mesage_lcd.WriteText(Convert.ToString(message.Data), true);
                mesage_lcd.WriteText(Convert.ToString(message.Tag), true);
                mesage_lcd.WriteText(Convert.ToString(message.Source), true);
            }

            if (edik.HasPendingMessage)
            {
                message = edik.AcceptMessage();
                mesage_lcd.WriteText("Broadcast");
                string mes_out = Convert.ToString(message.Data);
                //mesage_lcd.WriteText(mes_out);
                mesage_lcd.WriteText(Convert.ToString(message.Data), true);
                mesage_lcd.WriteText(Convert.ToString(message.Tag), true);
                mesage_lcd.WriteText(Convert.ToString(message.Source), true);
                if (!String.IsNullOrWhiteSpace(mes_out))
                {
                    string[] args = mes_out.Split('=');
                    if (args.Count() > 1 && !String.IsNullOrWhiteSpace(args[1]))
                    {
                        switch (args[0])
                        {                            
                            case "conn_cp":
                                {
                                    nav.DockMatrix = new MatrixD(mystorage.GetValDouble("DM11", args[1].ToString()), mystorage.GetValDouble("DM12", args[1].ToString()), mystorage.GetValDouble("DM13", args[1].ToString()), mystorage.GetValDouble("DM14", args[1].ToString()),
                                    mystorage.GetValDouble("DM21", args[1].ToString()), mystorage.GetValDouble("DM22", args[1].ToString()), mystorage.GetValDouble("DM23", args[1].ToString()), mystorage.GetValDouble("DM24", args[1].ToString()),
                                    mystorage.GetValDouble("DM31", args[1].ToString()), mystorage.GetValDouble("DM32", args[1].ToString()), mystorage.GetValDouble("DM33", args[1].ToString()), mystorage.GetValDouble("DM34", args[1].ToString()),
                                    mystorage.GetValDouble("DM41", args[1].ToString()), mystorage.GetValDouble("DM42", args[1].ToString()), mystorage.GetValDouble("DM43", args[1].ToString()), mystorage.GetValDouble("DM44", args[1].ToString()));
                                    StringBuilder values = new StringBuilder();
                                    Vector3D V3Dcenter = cockpit.obj.GetPosition();
                                    Vector3D V3Dfow = cockpit.obj.WorldMatrix.Forward + V3Dcenter;
                                    Vector3D V3Dup = cockpit.obj.WorldMatrix.Up + V3Dcenter;
                                    Vector3D V3Dleft = cockpit.obj.WorldMatrix.Left + V3Dcenter;

                                    // переводим в локальные
                                    V3Dcenter = Vector3D.Transform(V3Dcenter, nav.DockMatrix);
                                    V3Dfow = (Vector3D.Transform(V3Dfow, nav.DockMatrix)) - V3Dcenter;
                                    V3Dup = (Vector3D.Transform(V3Dup, nav.DockMatrix)) - V3Dcenter;
                                    V3Dleft = (Vector3D.Transform(V3Dleft, nav.DockMatrix)) - V3Dcenter;

                                    //values.Append(PText.GetGPS("V3Dcenter-gl", V3Dcenter) + "\n");
                                    //V3Dcenter = Vector3D.Transform(V3Dcenter, DockMatrix); // локальная
                                    //values.Append(PText.GetGPS("V3Dcenter-loc", V3Dcenter) + "\n");
                                    // 
                                    Vector3D point_conn_cp = Vector3D.Transform((new Vector3D(0, 0, 0) - V3Dcenter), cockpit.obj.WorldMatrix);
                                    Vector3D point_conn_cp_0 = Vector3D.Transform((new Vector3D(0, 0, 0)), cockpit.obj.WorldMatrix);
                                    Vector3D point_conn_cp_x = Vector3D.Transform((new Vector3D(10, 0, 0)), cockpit.obj.WorldMatrix);
                                    Vector3D point_conn_cp_y = Vector3D.Transform((new Vector3D(0, 10, 0)), cockpit.obj.WorldMatrix);
                                    Vector3D point_conn_cp_z = Vector3D.Transform((new Vector3D(0, 0, 10)), cockpit.obj.WorldMatrix);
                                    //
                                    values.Append(PText.GetGPS("p_conn_h1", point_conn_cp));
                                    values.Append(PText.GetGPS("p_conn_h1_0", point_conn_cp_0));
                                    values.Append(PText.GetGPS("p_conn_h1_x", point_conn_cp_x));
                                    values.Append(PText.GetGPS("p_conn_h1_y", point_conn_cp_y));
                                    values.Append(PText.GetGPS("p_conn_h1_z", point_conn_cp_z));
                                    lcd_dm.OutText(values);
                                    break;
                                };
                            case "conn_h1":
                                {
                                    nav.DockMatrix = new MatrixD(mystorage.GetValDouble("DM11", args[1].ToString()), mystorage.GetValDouble("DM12", args[1].ToString()), mystorage.GetValDouble("DM13", args[1].ToString()), mystorage.GetValDouble("DM14", args[1].ToString()),
                                    mystorage.GetValDouble("DM21", args[1].ToString()), mystorage.GetValDouble("DM22", args[1].ToString()), mystorage.GetValDouble("DM23", args[1].ToString()), mystorage.GetValDouble("DM24", args[1].ToString()),
                                    mystorage.GetValDouble("DM31", args[1].ToString()), mystorage.GetValDouble("DM32", args[1].ToString()), mystorage.GetValDouble("DM33", args[1].ToString()), mystorage.GetValDouble("DM34", args[1].ToString()),
                                    mystorage.GetValDouble("DM41", args[1].ToString()), mystorage.GetValDouble("DM42", args[1].ToString()), mystorage.GetValDouble("DM43", args[1].ToString()), mystorage.GetValDouble("DM44", args[1].ToString()));
                                    StringBuilder values = new StringBuilder();
                                    Vector3D V3Dcenter = cockpit.obj.GetPosition();
                                    Vector3D V3Dfow = cockpit.obj.WorldMatrix.Forward + V3Dcenter;
                                    Vector3D V3Dup = cockpit.obj.WorldMatrix.Up + V3Dcenter;
                                    Vector3D V3Dleft = cockpit.obj.WorldMatrix.Left + V3Dcenter;

                                    // переводим в локальные
                                    V3Dcenter = Vector3D.Transform(V3Dcenter, nav.DockMatrix);
                                    V3Dfow = (Vector3D.Transform(V3Dfow, nav.DockMatrix)) - V3Dcenter;
                                    V3Dup = (Vector3D.Transform(V3Dup, nav.DockMatrix)) - V3Dcenter;
                                    V3Dleft = (Vector3D.Transform(V3Dleft, nav.DockMatrix)) - V3Dcenter;

                                    //values.Append(PText.GetGPS("V3Dcenter-gl", V3Dcenter) + "\n");
                                    //V3Dcenter = Vector3D.Transform(V3Dcenter, DockMatrix); // локальная
                                    //values.Append(PText.GetGPS("V3Dcenter-loc", V3Dcenter) + "\n");
                                    // 
                                    Vector3D point_conn_h1 = Vector3D.Transform((new Vector3D(0, 0, 0) - V3Dcenter), cockpit.obj.WorldMatrix);
                                    Vector3D point_conn_h1_0 = Vector3D.Transform((new Vector3D(0, 0, 0)), cockpit.obj.WorldMatrix);
                                    Vector3D point_conn_h1_x = Vector3D.Transform((new Vector3D(10, 0, 0)), cockpit.obj.WorldMatrix);
                                    Vector3D point_conn_h1_y = Vector3D.Transform((new Vector3D(0, 10, 0)), cockpit.obj.WorldMatrix);
                                    Vector3D point_conn_h1_z = Vector3D.Transform((new Vector3D(0, 0, 10)), cockpit.obj.WorldMatrix);
                                    //
                                    values.Append(PText.GetGPS("p_conn_h1", point_conn_h1));
                                    values.Append(PText.GetGPS("p_conn_h1_0", point_conn_h1_0));
                                    values.Append(PText.GetGPS("p_conn_h1_x", point_conn_h1_x));
                                    values.Append(PText.GetGPS("p_conn_h1_y", point_conn_h1_y));
                                    values.Append(PText.GetGPS("p_conn_h1_z", point_conn_h1_z));
                                    lcd_dm.OutText(values);
                                    break;
                                };
                            case "conn_h2":
                                {
                                    nav.DockMatrix = new MatrixD(mystorage.GetValDouble("DM11", args[1].ToString()), mystorage.GetValDouble("DM12", args[1].ToString()), mystorage.GetValDouble("DM13", args[1].ToString()), mystorage.GetValDouble("DM14", args[1].ToString()),
                                    mystorage.GetValDouble("DM21", args[1].ToString()), mystorage.GetValDouble("DM22", args[1].ToString()), mystorage.GetValDouble("DM23", args[1].ToString()), mystorage.GetValDouble("DM24", args[1].ToString()),
                                    mystorage.GetValDouble("DM31", args[1].ToString()), mystorage.GetValDouble("DM32", args[1].ToString()), mystorage.GetValDouble("DM33", args[1].ToString()), mystorage.GetValDouble("DM34", args[1].ToString()),
                                    mystorage.GetValDouble("DM41", args[1].ToString()), mystorage.GetValDouble("DM42", args[1].ToString()), mystorage.GetValDouble("DM43", args[1].ToString()), mystorage.GetValDouble("DM44", args[1].ToString()));

                                    Vector3D V3Dcenter = cockpit.obj.GetPosition();  // Глобальная
                                    V3Dcenter = Vector3D.Transform(V3Dcenter, nav.DockMatrix); // локальная
                                    // 
                                    Vector3D point_conn_h2 = Vector3D.Transform((new Vector3D(0, 0, 0) - V3Dcenter), cockpit.obj.WorldMatrix);
                                    StringBuilder values = new StringBuilder();
                                    values.Append(PText.GetGPS("p_conn_h2", point_conn_h2) + "\n");
                                    lcd_dm.OutText(values);
                                    break;
                                };
                            case "conn_v1":
                                {
                                    nav.DockMatrix = new MatrixD(mystorage.GetValDouble("DM11", args[1].ToString()), mystorage.GetValDouble("DM12", args[1].ToString()), mystorage.GetValDouble("DM13", args[1].ToString()), mystorage.GetValDouble("DM14", args[1].ToString()),
                                    mystorage.GetValDouble("DM21", args[1].ToString()), mystorage.GetValDouble("DM22", args[1].ToString()), mystorage.GetValDouble("DM23", args[1].ToString()), mystorage.GetValDouble("DM24", args[1].ToString()),
                                    mystorage.GetValDouble("DM31", args[1].ToString()), mystorage.GetValDouble("DM32", args[1].ToString()), mystorage.GetValDouble("DM33", args[1].ToString()), mystorage.GetValDouble("DM34", args[1].ToString()),
                                    mystorage.GetValDouble("DM41", args[1].ToString()), mystorage.GetValDouble("DM42", args[1].ToString()), mystorage.GetValDouble("DM43", args[1].ToString()), mystorage.GetValDouble("DM44", args[1].ToString()));

                                    Vector3D V3Dcenter = cockpit.obj.GetPosition();  // Глобальная
                                    V3Dcenter = Vector3D.Transform(V3Dcenter, nav.DockMatrix); // локальная
                                    // 
                                    Vector3D point_conn_v1 = Vector3D.Transform((new Vector3D(0, 0, 0) - V3Dcenter), cockpit.obj.WorldMatrix);
                                    StringBuilder values = new StringBuilder();
                                    values.Append(PText.GetGPS("p_conn_v1", point_conn_v1) + "\n");
                                    lcd_dm.OutText(values);
                                    break;
                                };
                            default:
                                break;
                        }
                    }
                }


                //if (Convert.ToString(message.Data) == "test")
                //{
                //    IGC.SendBroadcastMessage<string>(tag, "Ok");//отправляю сообщение по нужному тегу
                //}
            }
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name)
            {
                if (base.obj != null)
                {
                    base.obj.SetValue("Content", (Int64)1);
                }
            }
            public void OutText(StringBuilder values)
            {
                if (base.obj != null)
                {
                    base.obj.WriteText(values, false);
                }
            }
            public void OutText(string text, bool append)
            {
                if (base.obj != null)
                {
                    base.obj.WriteText(text, append);
                }
            }
            public StringBuilder GetText()
            {
                StringBuilder values = new StringBuilder();
                if (base.obj != null)
                {
                    base.obj.ReadText(values);
                }
                return values;
            }
        }
        public class Cockpit : BaseController { public Cockpit(string name) : base(name) { } }
        public class Connector : BaseTerminalBlock<IMyShipConnector>
        {
            public MyShipConnectorStatus Status { get { return base.obj.Status; } }
            public bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name) : base(name) { if (base.obj != null) { } }
            public string TextStatus() { StringBuilder values = new StringBuilder(); values.Append(Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString())); return values.ToString(); }
            public string TextInfo(string name) { StringBuilder values = new StringBuilder(); values.Append((name != null ? name : "КОННЕКТОР") + " : " + TextStatus()); return values.ToString(); }
            public void Connect() { obj.Connect(); }
            public void Disconnect() { obj.Disconnect(); }
            public long? getEntityIdRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn.EntityId; } return null; }
            public IMyShipConnector getRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn; } return null; }
        }
        public class Navigation {

            public bool gravity = false;
            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D VelocityVector { get; private set; }
            public Vector3D UpVelocityVector { get; private set; }
            public Vector3D ForwVelocityVector { get; private set; }
            public Vector3D LeftVelocityVector { get; private set; }
            public Vector3D GravVector { get; private set; }
            public float PhysicalMass { get; private set; } // Физическая масса
            public float TotalMass { get; private set; } // Физическая масса
            public MatrixD WMCocpit { get; private set; } //
            public MatrixD OrientationCocpit { get; private set; } //
            public float XMaxA { get; private set; }
            public float YMaxA { get; private set; }
            public float ZMaxA { get; private set; }

            public MatrixD DockMatrix { get; set; }
            public Navigation()
            {

            }
            public MatrixD GetNormTransMatrixFromMyPos()
            {
                MatrixD mRot;
                Vector3D V3Dcenter = MyPos;
                Vector3D V3Dup = WMCocpit.Up;
                if (gravity) V3Dup = -Vector3D.Normalize(GravVector);
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.Reject(WMCocpit.Left, V3Dup));
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup));
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1);
                mRot = MatrixD.Invert(mRot);
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot;
            }
            public void SetDockMatrix()
            {

                {
                    DockMatrix = GetNormTransMatrixFromMyPos();
                    //mystorage.SaveToStorage();
                }
            }
            public void UpdateCalc()
            {
                MyPrevPos = MyPos;
                MyPos = cockpit.obj.GetPosition();
                GravVector = cockpit.obj.GetNaturalGravity();
                gravity = GravVector.LengthSquared() > 0.2f;
                PhysicalMass = cockpit.obj.CalculateShipMass().PhysicalMass;
                TotalMass = cockpit.obj.CalculateShipMass().TotalMass;
                WMCocpit = cockpit.obj.WorldMatrix;
                VelocityVector = (MyPos - MyPrevPos) * 6;
                UpVelocityVector = WMCocpit.Up * Vector3D.Dot(VelocityVector, WMCocpit.Up);
                ForwVelocityVector = WMCocpit.Forward * Vector3D.Dot(VelocityVector, WMCocpit.Forward);
                LeftVelocityVector = WMCocpit.Left * Vector3D.Dot(VelocityVector, WMCocpit.Left);
                OrientationCocpit = cockpit.GetCockpitMatrix();
                //YMaxA = Math.Abs((float)Math.Min(thrusts.UpThrMax / PhysicalMass - GravVector.Length(), thrusts.DownThrMax / PhysicalMass + GravVector.Length()));
                //ZMaxA = (float)Math.Min(thrusts.ForwardThrMax, thrusts.BackwardThrMax) / PhysicalMass;
                //XMaxA = (float)Math.Min(thrusts.RightThrMax, thrusts.LeftThrMax) / PhysicalMass;
                StringBuilder values = new StringBuilder();
                values.Append("pb : " + pb.EntityId + "\n");
                Vector3D MyPosPoint = Vector3D.Transform(MyPos, DockMatrix);
                values.Append("MyPos_Length   : " + Math.Round(MyPosPoint.Length(), 2) + "\n");
                values.Append("MyPos[0]   : " + Math.Round(MyPosPoint.GetDim(0), 2) + "\n");
                values.Append("MyPos[1]   : " + Math.Round(MyPosPoint.GetDim(1), 2) + "\n");
                values.Append("MyPos[2]   : " + Math.Round(MyPosPoint.GetDim(2), 2) + "\n");
                Vector3D MyPosConn = Vector3D.Transform(connector_forw.GetPosition(), DockMatrix);
                values.Append("Conn_Length   : " + Math.Round(MyPosConn.Length(), 2) + "\n");
                values.Append("ConnPos[0]   : " + Math.Round(MyPosConn.GetDim(0), 2) + "\n");
                values.Append("ConnPos[1]   : " + Math.Round(MyPosConn.GetDim(1), 2) + "\n");
                values.Append("ConnPos[2]   : " + Math.Round(MyPosConn.GetDim(2), 2) + "\n");
                //lcd_dm1.OutText(values);
                cockpit.OutText(values, 1);
            }

            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "load": mystorage.LoadFromStorage(); break;
                    case "save": mystorage.SaveToStorage(); break;
                    case "save_base": SetDockMatrix(); break;
                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    cockpit.Logic(argument, updateSource);
                    UpdateCalc();
                }
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                //StringBuilder str = lcd_storage.GetText();
                //navigation.curent_programm = (Navigation.programm)GetValInt("curent_programm", str.ToString());
                //navigation.curent_mode = (Navigation.mode)GetValInt("curent_mode", str.ToString());
                //navigation.paused = GetValBool("pause", str.ToString());
                //navigation.EmergencySetpoint = GetValBool("EmergencySetpoint", str.ToString());
                //navigation.DockMatrix = new MatrixD(GetValDouble("DM11", str.ToString()), GetValDouble("DM12", str.ToString()), GetValDouble("DM13", str.ToString()), GetValDouble("DM14", str.ToString()),
                //GetValDouble("DM21", str.ToString()), GetValDouble("DM22", str.ToString()), GetValDouble("DM23", str.ToString()), GetValDouble("DM24", str.ToString()),
                //GetValDouble("DM31", str.ToString()), GetValDouble("DM32", str.ToString()), GetValDouble("DM33", str.ToString()), GetValDouble("DM34", str.ToString()),
                //GetValDouble("DM41", str.ToString()), GetValDouble("DM42", str.ToString()), GetValDouble("DM43", str.ToString()), GetValDouble("DM44", str.ToString()));
                //navigation.PlanetCenter = new Vector3D(GetValDouble("PX", str.ToString()), GetValDouble("PY", str.ToString()), GetValDouble("PZ", str.ToString()));
            }
            public void SaveToStorage()
            {
                //StringBuilder values = new StringBuilder();
                //values.Append("curent_programm: " + ((int)navigation.curent_programm).ToString() + ";\n");
                //values.Append("curent_mode: " + ((int)navigation.curent_mode).ToString() + ";\n");
                //values.Append("pause: " + navigation.paused.ToString() + ";\n");
                //values.Append("EmergencySetpoint: " + navigation.EmergencySetpoint.ToString() + ";\n");
                //values.Append(navigation.DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "DM"));
                //values.Append(navigation.PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX").Replace("Y", "PY").Replace("Z", "PZ") + ";\n");
                //lcd_storage.OutText(values);
            }
            private string GetVal(string Key, string str, string val) { string pattern = @"(" + Key + "):([^:^;]+);"; System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern); if (match.Success) { val = match.Groups[2].Value; } return val; }
            public string GetValString(string Key, string str) { return GetVal(Key, str, ""); }
            public double GetValDouble(string Key, string str) { return Convert.ToDouble(GetVal(Key, str, "0")); }
            public int GetValInt(string Key, string str) { return Convert.ToInt32(GetVal(Key, str, "0")); }
            public long GetValInt64(string Key, string str) { return Convert.ToInt64(GetVal(Key, str, "0")); }
            public bool GetValBool(string Key, string str) { return Convert.ToBoolean(GetVal(Key, str, "False")); }
        }
    }
}


//121268418639130970
//121268418639130970
