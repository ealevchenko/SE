using Newtonsoft.Json.Linq;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
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
using VRageMath;

namespace IGC_Antena_Basa
{
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[BT]";

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static IMyTextPanel mesage_lcd;

        IMyRadioAntenna antenna;
        IMyProgrammableBlock pb;


        static IMyBroadcastListener edik;
        static IMyUnicastListener edik1;
        static string tag = "chanel";
        static MyIGCMessage message;
        static LCD lcd_dm;
        static Connector connector_h1;
        static Connector connector_h2;
        static Connector connector_v1;
        static Cockpit cockpit;
        static Upr upr;
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

            antenna = GridTerminalSystem.GetBlockWithName("Antenna") as IMyRadioAntenna;
            pb = GridTerminalSystem.GetBlockWithName("Programmable block") as IMyProgrammableBlock;

            //antenna.AttachedProgrammableBlock = pb.EntityId;

            edik = IGC.RegisterBroadcastListener(tag);
            edik1 = IGC.UnicastListener;
            message = new MyIGCMessage();
            connector_h1 = new Connector(NameObj + "-Коннектор h1");
            connector_h2 = new Connector(NameObj + "-Коннектор h2");
            connector_v1 = new Connector(NameObj + "-Коннектор v1");
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD] Locked");
            upr = new Upr();
        }

        void Main(string argument, UpdateType updateSource)
        {
            upr.Logic(argument, updateSource);


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
        public class Cockpit : BaseController { public Cockpit(string name) : base(name) { } }
        public class Upr
        {
            public Vector3D GravVector { get; private set; }
            public bool gravity { get; private set; } = false;
            public Vector3D PlanetCenter = new Vector3D(0.50, 0.50, 0.50);

            public void FindPlanetCenter()
            {
                if (cockpit.obj.TryGetPlanetPosition(out PlanetCenter))
                {

                }
            }
            public MatrixD GetNormTransMatrixFromMyPos(Connector my_conn)
            {
                MatrixD mRot;
                StringBuilder values = new StringBuilder();


                Vector3D V3Dcenter = my_conn.obj.GetPosition();
                Vector3D V3Dup = my_conn.obj.WorldMatrix.Up;
                if (gravity) V3Dup = -Vector3D.Normalize(GravVector);
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.Reject(my_conn.obj.WorldMatrix.Right, V3Dup));
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup));
                values.Append(PText.GetGPS("V3Dcenter", V3Dcenter));
                values.Append(PText.GetGPS("V3Dup", V3Dup));
                values.Append(PText.GetGPS("V3Dleft", V3Dleft));
                values.Append(PText.GetGPS("V3Dfow", V3Dfow));
                lcd_dm.OutText(values);
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1);
                mRot = MatrixD.Invert(mRot);
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot;
            }
            public MatrixD GetNormTransMatrixFromPoint(Vector3D Point)
            {
                MatrixD mRot;
                Vector3D V3Dcenter = Point;
                Vector3D V3Dup = Vector3D.Normalize(V3Dcenter - PlanetCenter);
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.CalculatePerpendicularVector(V3Dup));
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup));
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1);
                mRot = MatrixD.Invert(mRot);
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot;
            }

            public Upr()
            {
                FindPlanetCenter();
            }

            public void UpdateCall()
            {
                GravVector = cockpit.obj.GetNaturalGravity();
                gravity = GravVector.LengthSquared() > 0.2f;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                UpdateCall();


                // To unicast a message to our friend, we need an address for his Programmable Block.
                // We'll pretend here that he has copied it and sent it to us via Steam chat.
                long friendAddress = 3672132753819237;

                //// Here, we'll use the tag to convey information about what we're sending to our friend.
                //string tagUni = "Int";

                //// We're sending a number instead of a string.
                //int number = 1337;

                //// We access the unicast method through IGC and input our address, tag and data.
                //_scr.IGC.SendUnicastMessage(friendAddress, tagUni, number);
                //_scr.IGC.SendUnicastMessage(friendAddress, "Int", "hhhhhhhh");

                switch (argument)
                {
                    //case "load": mystorage.LoadFromStorage(); break;
                    //case "save": mystorage.SaveToStorage(); break;
                    case "test": _scr.IGC.SendBroadcastMessage<string>(tag, argument); break;
                    case "test_uc": _scr.IGC.SendUnicastMessage<string>(friendAddress, "Test", argument); break;
                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    if (edik1.HasPendingMessage)
                    {
                        message = edik.AcceptMessage();
                        mesage_lcd.WriteText("Unicast");
                        mesage_lcd.WriteText(Convert.ToString(message.Data) + "\n", true);
                        mesage_lcd.WriteText(Convert.ToString(message.Tag) + "\n", true);
                        mesage_lcd.WriteText(Convert.ToString(message.Source) + "\n", true);

                    }

                    if (edik.HasPendingMessage)
                    {
                        message = edik.AcceptMessage();
                        mesage_lcd.WriteText("Broadcast");
                        mesage_lcd.WriteText(Convert.ToString(message.Data) + "\n", true);
                        mesage_lcd.WriteText(Convert.ToString(message.Tag) + "\n", true);
                        mesage_lcd.WriteText(Convert.ToString(message.Source) + "\n", true);

                        if (Convert.ToString(message.Data) == "test_privat")
                        {
                            _scr.IGC.SendUnicastMessage<string>(message.Source, "responce", "test_privat=" + message.Source);
                        }
                        if (Convert.ToString(message.Data) == "conn_h1")
                        {
                            MatrixD conn_matr = GetNormTransMatrixFromMyPos(connector_h1);
                            //MatrixD conn_matr = GetNormTransMatrixFromPoint(connector_h1.GetPosition());
                            StringBuilder values = new StringBuilder();
                            values.Append(conn_matr.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "DM"));
                            //lcd_dm.OutText(values);
                            _scr.IGC.SendBroadcastMessage<string>(tag, "conn_h1=" + values.ToString());//отправляю сообщение по нужному тегу
                        }
                        if (Convert.ToString(message.Data) == "conn_h2")
                        {
                            MatrixD conn_matr = GetNormTransMatrixFromMyPos(connector_h2);
                            //MatrixD conn_matr = GetNormTransMatrixFromPoint(connector_h2.GetPosition());
                            StringBuilder values = new StringBuilder();
                            values.Append(conn_matr.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "DM"));
                            //lcd_dm.OutText(values);
                            _scr.IGC.SendBroadcastMessage<string>(tag, "conn_h2=" + values.ToString());//отправляю сообщение по нужному тегу
                        }
                        if (Convert.ToString(message.Data) == "conn_v1")
                        {
                            MatrixD conn_matr = GetNormTransMatrixFromMyPos(connector_v1);
                            //MatrixD conn_matr = GetNormTransMatrixFromPoint(connector_v1.GetPosition());
                            StringBuilder values = new StringBuilder();
                            values.Append(conn_matr.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "DM"));
                            //lcd_dm.OutText(values);
                            _scr.IGC.SendBroadcastMessage<string>(tag, "conn_v1=" + values.ToString());//отправляю сообщение по нужному тегу
                        }
                    }
                }
            }
        }
        public class Ports
        {
            public Ports()
            {

            }
        }
    }
}
