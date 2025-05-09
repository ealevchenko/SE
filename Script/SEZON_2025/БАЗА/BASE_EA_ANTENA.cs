using Newtonsoft.Json.Linq;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRage.Scripting;
using VRageMath;
using static СORVETTE_H1_DOOR.Program;


/// <summary>
/// v1.0
/// База (планета земля) - управление сообщениями V1.09-05-2025
/// </summary>
namespace BASE_EA_ANTENA
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        static string NameObj = "[BER-01]";
        static string tag_antena = "[antena]";
        static int type_base = 1; // планетарная

        public static Color red = new Color(255, 0, 0);
        public static Color yellow = new Color(255, 255, 0);
        public static Color green = new Color(0, 128, 0);

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static LCD lcd_storage;
        static LCD lcd_lstr;
        static LCD lcd_info, lcd_debug;
        static MessHandler mess_handler;

        static MyStorage storage;

        static Program _scr;

        public class PItem
        {
            public class ItN
            {
                public string SubtypeId { get; }
                public string mainType { get; }
                public string Name { get; }
                public ItN() { }
                public ItN(string SubtypeId, string mainType, string Name) { this.SubtypeId = SubtypeId; this.mainType = mainType; this.Name = Name; }
            }

            static public List<ItN> lcn = new List<ItN>() {
                new ItN("Stone" ,"Ore"  ,"Камень"),
                new ItN("Iron"  ,"Ore"  ,"Железо"),
                new ItN("Nickel","Ore"  ,"Никель"),
                new ItN("Cobalt","Ore"  ,"Кобальт"  ),
                new ItN("Magnesium" ,"Ore"  ,"Магний"),
                new ItN("Silicon","Ore"  ,"Кремний"  ),
                new ItN("Silver","Ore"  ,"Серебро"  ),
                new ItN("Gold"  ,"Ore"  ,"Золото"),
                new ItN("Platinum"  ,"Ore"  ,"Платина"  ),
                new ItN("Uranium","Ore"  ,"Уран" ),
                new ItN("Ice","Ore"  ,"Лед"  ),
                new ItN("Scrap" ,"Ore"  ,"Металлолом"),
                new ItN("Stone" ,"Ingot","Гравий"),
                new ItN("Iron"  ,"Ingot","Железо"),
                new ItN("Nickel","Ingot","Никель"),
                new ItN("Cobalt","Ingot","Кобальт"  ),
                new ItN("Magnesium" ,"Ingot","Магний"),
                new ItN("Silicon","Ingot","Кремний"  ),
                new ItN("Silver","Ingot","Серебро"  ),
                new ItN("Gold"  ,"Ingot","Золото"),
                new ItN("Platinum"  ,"Ingot","Платина"  ),
                new ItN("Uranium","Ingot","Уран" ),
                new ItN("SemiAutoPistolItem","Tool" ,"S-10 Пистолет"),
                new ItN("ElitePistolItem","Tool" ,"S-10E Пистолет"),
                new ItN("FullAutoPistolItem","Tool" ,"S-20A Пистолет"),
                new ItN("AutomaticRifleItem","Tool" ,"MR-20 Винтовка"),
                new ItN("PreciseAutomaticRifleItem" ,"Tool" ,"MR-8P Винтовка"),
                new ItN("RapidFireAutomaticRifleItem","Tool" ,"MR-50A Винтовка"  ),
                new ItN("UltimateAutomaticRifleItem","Tool" ,"MR-30E Винтовка"  ),
                new ItN("BasicHandHeldLauncherItem" ,"Tool" ,"RO-1 Ракетница"),
                new ItN("AdvancedHandHeldLauncherItem"  ,"Tool" ,"PRO-1 Ракетница"  ),
                new ItN("WelderItem","Tool" ,"Сварщик"  ),
                new ItN("Welder2Item","Tool" ,"* Улучшенный сварщик" ),
                new ItN("Welder3Item","Tool" ,"** Продинутый сварщик"),
                new ItN("Welder4Item","Tool" ,"*** Элитный сварщик"  ),
                new ItN("AngleGrinderItem"  ,"Tool" ,"Резак"),
                new ItN("AngleGrinder2Item" ,"Tool" ,"* Улучшенная болгарка"),
                new ItN("AngleGrinder3Item" ,"Tool" ,"** Продинутая болгарка"),
                new ItN("AngleGrinder4Item" ,"Tool" ,"*** Элитная болгарка" ),
                new ItN("HandDrillItem" ,"Tool" ,"Ручной бур"),
                new ItN("HandDrill2Item","Tool" ,"* Улучшенный ручной бур"  ),
                new ItN("HandDrill3Item","Tool" ,"** Продинутый ручной бур" ),
                new ItN("HandDrill4Item","Tool" ,"*** Элитный ручной бур"),
                new ItN("FlareGunItem"  ,"Tool" ,"Flare Gun"),
                new ItN("Construction"  ,"Component","Стройкомпоненты"  ),
                new ItN("MetalGrid" ,"Component","Компонет решётки" ),
                new ItN("InteriorPlate" ,"Component","Внутренная пластина"  ),
                new ItN("SteelPlate","Component","Стальная пластина"),
                new ItN("Girder","Component","Балка"),
                new ItN("SmallTube" ,"Component","Малая труба"  ),
                new ItN("LargeTube" ,"Component","Большая труба"),
                new ItN("Motor" ,"Component","Мотор"),
                new ItN("Display","Component","Экран"),
                new ItN("BulletproofGlass"  ,"Component","Бронированное стекло" ),
                new ItN("Computer"  ,"Component","Компьютер"),
                new ItN("Reactor","Component","Компоненты реактора"  ),
                new ItN("Thrust","Component","Детали ускорителя"),
                new ItN("GravityGenerator"  ,"Component","Гравикомпоненты"  ),
                new ItN("Medical","Component","Медкомпоненты"),
                new ItN("RadioCommunication","Component","Радиокомпоненты"  ),
                new ItN("Detector"  ,"Component","Компоненты детектора" ),
                new ItN("Explosives","Component","Взырвчатка"),
                new ItN("SolarCell" ,"Component","Солнечная ячейка" ),
                new ItN("PowerCell" ,"Component","Энергоячека"  ),
                new ItN("Superconductor","Component","Сверхпроводник"),
                new ItN("Canvas","Component","Полотно парашюта" ),
                new ItN("EngineerPlushie","Component","Плюшевый Инженер" ),
                new ItN("SabiroidPlushie","Component","Плюшевый Сабироид"),
                new ItN("ZoneChip"  ,"Component","Чип"  ),
                new ItN("Datapad","Datapad"  ,"Инфопланшет"  ),
                new ItN("Package","Package"  ,"Упаковка" ),
                new ItN("Medkit","ConsumableItem","Аптечка"  ),
                new ItN("Powerkit"  ,"ConsumableItem","Внешний аккумулятор " ),
                new ItN("ClangCola" ,"ConsumableItem","Кола" ),
                new ItN("CosmicCoffee"  ,"ConsumableItem","Кофе" ),
                new ItN("SpaceCredit","PhysicalObject","Кредиты"  ),
                new ItN("NATO_5p56x45mm","Ammo" ,"5.56x45mm"),
                new ItN("SemiAutoPistolMagazine","Ammo" ,"S-10 Mag" ),
                new ItN("ElitePistolMagazine","Ammo" ,"S-10E Mag"),
                new ItN("FullAutoPistolMagazine","Ammo" ,"S-20A Mag"),
                new ItN("AutomaticRifleGun_Mag_20rd","Ammo" ,"MR-20 Mag"),
                new ItN("PreciseAutomaticRifleGun_Mag_5rd"  ,"Ammo" ,"MR-8P Mag"),
                new ItN("RapidFireAutomaticRifleGun_Mag_50rd","Ammo" ,"MR-50A Mag"),
                new ItN("UltimateAutomaticRifleGun_Mag_30rd","Ammo" ,"MR-30E Mag"),
                new ItN("NATO_25x184mm" ,"Ammo" ,"Гатлинг патроны"  ),
                new ItN("Missile200mm"  ,"Ammo" ,"200мм ракета" ),
                new ItN("AutocannonClip","Ammo" ,"М-н автопушки"),
                new ItN("MediumCalibreAmmo" ,"Ammo" ,"Снаряд ШП"),
                new ItN("SmallRailgunAmmo"  ,"Ammo" ,"МС Рельсотрон"),
                new ItN("LargeRailgunAmmo"  ,"Ammo" ,"БС Рельсотрон"),
                new ItN("LargeCalibreAmmo"  ,"Ammo" ,"АРТ Снаряд"),
                new ItN("FlareClip" ,"Ammo" ,"Flare Clip"),
                new ItN("OxygenBottle"  ,"OxygenContainerObject","Кислородные баллоны"  ),
                new ItN("HydrogenBottle","GasContainerObject","Водородные баллоны"),
                new ItN("AzimuthSupercharger","Component","Supercharger" ),
                new ItN("OKI23mmAmmo","Ammo" ,"23x180mm" ),
                new ItN("OKI50mmAmmo","Ammo" ,"50x450mm" ),
                new ItN("OKI122mmAmmo"  ,"Ammo" ,"122x640mm"),
                new ItN("OKI230mmAmmo"  ,"Ammo" ,"230x920mm")
            };
            static public string getName(string SubtypeId) { ItN res = PItem.lcn.Find(n => n.SubtypeId == SubtypeId); return res != null ? res.Name : SubtypeId; }
            static public List<ItN> getListType(string mainType) { return PItem.lcn.Where(t => t.mainType == mainType).ToList(); }
        }
        public class PText
        {
            static public string GetPersent(double perse) { return " - " + Math.Round((perse * 100), 1) + "%"; }
            static public string GetScalePersent(double perse, int scale) { string prog = "["; for (int i = 0; i < Math.Round((perse * scale), 0); i++) { prog += "|"; } for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++) { prog += "'"; } prog += "]" + GetPersent(perse); return prog; }
            static public string GetCurrentOfMax(float cur, float max, string units) { return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetCurrentOfMinMax(float min, float cur, float max, string units) { return "[ " + Math.Round(min, 1) + units + " / " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetThrust(float value) { return Math.Round(value / 1000000, 1) + "МН"; }
            static public string GetFarm(float value) { return Math.Round(value, 1) + "L"; }
            static public string GetMass(float value, string units) { return value.ToString() + units; }
            static public string GetGPS(string name, Vector3D target) { return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n"; }
            static public string GetGPSMatrixD(string name, MatrixD target) { return "MatrixD:" + name + "\n" + "M11:" + target.M11 + "M12:" + target.M12 + "M13:" + target.M13 + "M14:" + target.M14 + ":\n" + "M21:" + target.M21 + "M22:" + target.M22 + "M23:" + target.M23 + "M24:" + target.M24 + ":\n" + "M31:" + target.M31 + "M32:" + target.M32 + "M33:" + target.M33 + "M34:" + target.M34 + ":\n" + "M41:" + target.M41 + "M42:" + target.M42 + "M43:" + target.M43 + "M44:" + target.M44 + ":\n"; }
        }
        public class BaseListTerminalBlock<T> where T : class
        {
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj) { _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj)); _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "]: " + list_obj.Count()); }
            public BaseListTerminalBlock(string name_obj, string tag) { _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj)); if (!String.IsNullOrWhiteSpace(tag)) { list_obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).ToList(); } _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "],[" + tag + "]: " + list_obj.Count()); }
            private void Off(List<T> list) { foreach (IMyTerminalBlock obj in list) { obj.ApplyAction("OnOff_Off"); } }
            public void Off() { Off(list_obj); }
            private void OffOfTag(List<T> list, string tag) { foreach (IMyTerminalBlock obj in list) { if (obj.CustomName.Contains(tag)) { obj.ApplyAction("OnOff_Off"); } } }
            public void OffOfTag(string tag) { OffOfTag(list_obj, tag); }
            private void On(List<T> list) { foreach (IMyTerminalBlock obj in list) { obj.ApplyAction("OnOff_On"); } }
            public void On() { On(list_obj); }
            public void OnOff(bool on_off) { if (on_off) On(); else Off(); }
            private void OnOfTag(List<T> list, string tag)
            { foreach (IMyTerminalBlock obj in list) { if (obj.CustomName.Contains(tag)) { obj.ApplyAction("OnOff_On"); } } }
            public void OnOfTag(string tag) { OnOfTag(list_obj, tag); }
            public bool Enabled(string tag) { foreach (IMyTerminalBlock obj in list_obj) { if (obj.CustomName.Contains(tag) && !((IMyFunctionalBlock)obj).Enabled) { return false; } } return true; }
            public bool Enabled() { foreach (IMyTerminalBlock obj in list_obj) { if (!((IMyFunctionalBlock)obj).Enabled) { return false; } } return true; }
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public string CustomName { get { return ((IMyTerminalBlock)this.obj).CustomName; } set { ((IMyTerminalBlock)this.obj).CustomName = value; } }
            public BaseTerminalBlock(string name) { obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T; _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public BaseTerminalBlock(T myobj) { obj = myobj; _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block"))); }
            public Vector3D GetPosition() { return ((IMyEntity)obj).GetPosition(); }
            public void Off() { if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off"); }
            public void On() { if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On"); }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage-antena]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG-ANT");
            lcd_info = new LCD(NameObj + "-LCD-INFO-ANT");
            lcd_lstr = new LCD(NameObj + "-LCD-Listener");
            mess_handler = new MessHandler(NameObj);
            storage = new MyStorage();
            storage.LoadFromStorage();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument) { default: break; }
            mess_handler.Logic(argument, updateSource); // обработаем сообщенияя
            if (updateSource == UpdateType.Update100)
            {

            }
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name) { if (base.obj != null) { base.obj.SetValue("Content", (Int64)1); } }
            public void OutText(StringBuilder values) { if (base.obj != null) { base.obj.WriteText(values, false); } }
            public void OutText(string text, bool append) { if (base.obj != null) { base.obj.WriteText(text, append); } }
            public StringBuilder GetText() { StringBuilder values = new StringBuilder(); if (base.obj != null) { base.obj.ReadText(values); } return values; }
        }
        public class MessHandler
        {
            public string name_base { get; set; }
            public enum type_ship : int
            {
                none = 0,
                v_miner = 1,    // верт. буровик
                h_miner = 2,    // гор. буровик
                welder = 3,     // сварщик
                cutter = 4,     // резак
                truck = 5,      // грузовик
                rocket = 6,     // ракето-носитель
                scout = 7,      // разветчик
                corvette = 8,   // корвет
                drone = 9,      // дрон
            };
            public static string[] name_type_ship = { "?", "В-Буровик", "Г-Буровик", "Сварщик", "Резак", "Грузовик", "Ракетоноситель", "Разведчик", "Корвет", "Дрон" };
            public class ships
            {
                public string name { get; set; }
                public long addr { get; set; }
                public type_ship type { get; set; }
                public string thruster { get; set; }
            }

            public List<ships> list_ships = new List<ships>();

            public IMyRadioAntenna antenna;
            public IMyProgrammableBlock pb;
            public IMyUnicastListener base_lstr; // Одноадресный прослушиватель базы
            public MyIGCMessage message;
            public long pb_address { get; set; }
            public MessHandler(string name)
            {
                name_base = name;
                base_lstr = _scr.IGC.UnicastListener;
                List<IMyRadioAntenna> list_anten = new List<IMyRadioAntenna>();
                List<IMyProgrammableBlock> list_pb = new List<IMyProgrammableBlock>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list_anten, r => r.CustomName.Contains(name));
                _scr.Echo("MessHandler : Найдено IMyRadioAntenna - " + list_anten.Count());
                _scr.GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(list_pb, r => r.CustomName.Contains(name));
                pb = list_pb.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag_antena)).FirstOrDefault();
                _scr.Echo("MessHandler : IMyProgrammableBlock " + tag_antena + " - " + ((pb != null) ? ("Найден") : ("Ошибка")));
                pb_address = pb != null ? pb.EntityId : 0;
                //storage.SaveToStorage();
            }
            public bool Registration(String str, long addr)
            {
                string name = storage.GetValString("name", str);
                type_ship type = (type_ship)storage.GetValInt("type", str);
                string thruster = storage.GetValString("thruster", str);
                ships sh = list_ships.Where(s => s.addr == addr).FirstOrDefault();
                if (sh == null)
                {
                    sh = new ships()
                    {
                        name = name,
                        type = type,
                        addr = addr,
                        thruster = thruster,
                    };
                    list_ships.Add(sh);
                }
                else
                {
                    sh.name = name; sh.type = type; sh.thruster = thruster;
                }
                storage.SaveToStorage();
                return true;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "test": _scr.IGC.SendUnicastMessage<string>(pb_address, "tag", argument); break;
                    default: break;
                }
                if (updateSource == UpdateType.Update100)
                {
                    if (base_lstr.HasPendingMessage)
                    {
                        message = base_lstr.AcceptMessage();
                        StringBuilder values = new StringBuilder();
                        string mess_inp = Convert.ToString(message.Data);
                        string mess_tag = Convert.ToString(message.Tag);
                        string mess_source = Convert.ToString(message.Source);
                        long addr = !String.IsNullOrWhiteSpace(mess_source) ? Convert.ToInt64(mess_source) : 0;
                        values.Append("Data   : " + mess_inp + "\n");
                        values.Append("Tag    : " + mess_tag + "\n");
                        values.Append("Source : " + mess_source + "\n");
                        lcd_lstr.OutText(values);
                        string[] args = mess_inp.Split('=');
                        if (args.Count() > 0)
                        {
                            switch (args[0])
                            {
                                case "upd_ship":
                                    {
                                        bool res = Registration(args[1], addr);
                                        if (res)
                                        {
                                            string response = String.Format("upd_base=name:{0};type:{1};", name_base, type_base);
                                            _scr.IGC.SendUnicastMessage<string>(addr, name_base, response);
                                        }
                                        break;
                                    }
                                    ;
                            }
                        }
                    }
                }
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                //upr.curent_mode = (Nav.mode)GetValInt("curent_mode", str.ToString());
                //upr.horizont = GetValBool("horizont", str.ToString());
                //upr.FlyHeight = GetValDouble("FlyHeight", str.ToString());
                //upr.ShaftN = GetValInt("ShaftN", str.ToString());
                //upr.DockMatrix = GetValMatrixD("DM", str.ToString());
                //upr.PlanetCenter = GetValVector3D("PC", str.ToString());
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                //values.Append("curent_mode: " + ((int)nav.curent_mode).ToString() + ";\n");
                //values.Append("horizont: " + nav.horizont.ToString() + ";\n");
                //values.Append("FlyHeight: " + Math.Round(nav.FlyHeight, 0) + ";\n");
                //values.Append("ShaftN: " + nav.ShaftN.ToString() + ";\n");
                //values.Append(SetValMatrixD("DM", nav.DockMatrix) + ";\n");
                //values.Append(SetValVector3D("PC", nav.PlanetCenter) + ";\n");
                lcd_storage.OutText(values);
            }
            private string GetVal(string Key, string str, string val) { string pattern = @"(" + Key + "):([^:^;]+);"; System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern); if (match.Success) { val = match.Groups[2].Value; } return val; }
            public string GetValString(string Key, string str) { return GetVal(Key, str, ""); }
            public double GetValDouble(string Key, string str) { return Convert.ToDouble(GetVal(Key, str, "0")); }
            public int GetValInt(string Key, string str) { return Convert.ToInt32(GetVal(Key, str, "0")); }
            public long GetValInt64(string Key, string str) { return Convert.ToInt64(GetVal(Key, str, "0")); }
            public bool GetValBool(string Key, string str) { return Convert.ToBoolean(GetVal(Key, str, "False")); }
            public MatrixD GetValMatrixD(string Key, string str)
            {
                return new MatrixD(GetValDouble(Key + "11", str.ToString()), GetValDouble(Key + "12", str.ToString()), GetValDouble(Key + "13", str.ToString()), GetValDouble(Key + "14", str.ToString()),
                GetValDouble(Key + "21", str.ToString()), GetValDouble(Key + "22", str.ToString()), GetValDouble(Key + "23", str.ToString()), GetValDouble(Key + "24", str.ToString()),
                GetValDouble(Key + "31", str.ToString()), GetValDouble(Key + "32", str.ToString()), GetValDouble(Key + "33", str.ToString()), GetValDouble(Key + "34", str.ToString()),
                GetValDouble(Key + "41", str.ToString()), GetValDouble(Key + "42", str.ToString()), GetValDouble(Key + "43", str.ToString()), GetValDouble(Key + "44", str.ToString()));
            }
            public Vector3D GetValVector3D(string Key, string str) { return new Vector3D(GetValDouble(Key + "X", str.ToString()), GetValDouble(Key + "Y", str.ToString()), GetValDouble(Key + "Z", str.ToString())); }
            public string SetValVector3D(string Key, Vector3D val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", Key + "X").Replace("Y", Key + "Y").Replace("Z", Key + "Z"); }
            public string SetValMatrixD(string Key, MatrixD val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", Key); }
        }

    }
}
