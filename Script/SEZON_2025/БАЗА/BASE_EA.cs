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
/// База (планета земля) V1.07-02-2025
/// </summary>
namespace BASE_EA
{
    public sealed class Program : MyGridProgram
    {
        // v1.
        static string NameObj = "[BER-01]";
        static string tag_ref = "ref"; // [ref1]
        static string tag_led = "led"; // [led] [ref1]
        public enum room : int
        {
            none = 0,
            assembly = 1,
        };
        public static string[] name_room = { "", "СБОРОЧНЫЙ" };
        public static int[] count_room = { 0, 0 };

        public static float speed_piston_wg = 1.0f;       // один оборот в минуту

        public static Color red = new Color(255, 0, 0);
        public static Color yellow = new Color(255, 255, 0);
        public static Color green = new Color(0, 128, 0);

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static LCD lcd_storage;
        static LCD lcd_debug;
        static LCD lcd_info1;
        static LCD lcd_refinery;
        //static Lighting led_ref1, led_ref2, led_ref3, led_ref4;
        static Lightings leds;
        static Refinerys refs;
        static Batterys bats;
        static Upr upr;

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
            static public string getName(string SubtypeId) { ItN res = PItem.lcn.Find(n => n.SubtypeId == SubtypeId); return res != null ? res.Name : ""; }
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
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_info1 = new LCD(NameObj + "-LCD-INFO");
            lcd_refinery = new LCD(NameObj + "-LCD-Refinery");
            bats = new Batterys(NameObj);
            refs = new Refinerys(NameObj);
            leds = new Lightings(NameObj, tag_led);
            upr = new Upr();
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
            //count_room[(int)room.none] = 0;// В космосе людей не считаем
            upr.Logic(argument, updateSource);// Логика системы контроля питания
            if (updateSource == UpdateType.Update10)
            {

            }
            StringBuilder values = new StringBuilder();
            values.Append(bats.TextInfo(null));
            lcd_info1.OutText(values);
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name) { if (base.obj != null) { base.obj.SetValue("Content", (Int64)1); } }
            public void OutText(StringBuilder values) { if (base.obj != null) { base.obj.WriteText(values, false); } }
            public void OutText(string text, bool append) { if (base.obj != null) { base.obj.WriteText(text, append); } }
            public StringBuilder GetText() { StringBuilder values = new StringBuilder(); if (base.obj != null) { base.obj.ReadText(values); } return values; }
        }
        public class Lighting : BaseTerminalBlock<IMyLightingBlock>
        {
            public Lighting(string name_obj) : base(name_obj) { }
        }
        public class Lightings : BaseListTerminalBlock<IMyLightingBlock>
        {
            public Lightings(string name_obj) : base(name_obj) { }
            public Lightings(string name_obj, string tag) : base(name_obj, tag) { }
        }
        public class Refinerys : BaseListTerminalBlock<IMyRefinery>
        {
            public Refinerys(string name_obj) : base(name_obj) { }
            public Refinerys(string name_obj, string tag) : base(name_obj, tag) { }
            public string TextInfo(string tag)
            {
                StringBuilder values = new StringBuilder();
                IMyRefinery obj = base.list_obj.Find(r => r.CustomName.Contains(tag));
                if (obj != null)
                {
                    values.Append(obj.CustomName + "\n");
                    values.Append("ВКЛ.: " + (obj.IsWorking ? igreen.ToString() : ired.ToString()) + ", ");
                    values.Append("РАБ.: " + (obj.IsProducing ? igreen.ToString() : ired.ToString()) + ", ");
                    values.Append("ПУСТ: " + (obj.IsQueueEmpty ? igreen.ToString() : iyellow.ToString()) + "\n");
                    var inpItems = new List<MyInventoryItem>();
                    var outItems = new List<MyInventoryItem>();
                    obj.InputInventory.GetItems(inpItems);
                    obj.OutputInventory.GetItems(outItems);
                    values.Append("ВХОД :\n");
                    foreach (MyInventoryItem itm in inpItems)
                    {
                        values.Append(" - " + PItem.getName(itm.Type.SubtypeId) + ": " + PText.GetMass((float)itm.Amount, " кг.") + "\n");
                    }
                    values.Append("ВЫХОД : \n");
                    foreach (MyInventoryItem itm in outItems)
                    {
                        values.Append(" - " + PItem.getName(itm.Type.SubtypeId) + ": " + PText.GetMass((float)itm.Amount, " кг.") + "\n");
                    }
                }


                return values.ToString();
            }
        }
        public class Batterys : BaseListTerminalBlock<IMyBatteryBlock>
        {
            public float MaxPower { get { return base.list_obj.Select(b => b.MaxStoredPower).Sum(); } }
            public float CurrentPower { get { return base.list_obj.Select(b => b.CurrentStoredPower).Sum(); } }
            public float CurrentPersent { get { return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum(); } }
            public float CountCharger { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList().Count(); } }
            public float CountAuto { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList().Count(); } }
            public bool IsCharger { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList().Count() > 0; } }
            public bool IsAuto { get { return base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList().Count() > 0; } }
            public Batterys(string name_obj) : base(name_obj) { }
            public Batterys(string name_obj, string tag) : base(name_obj, tag) { }
            public void Charger() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Recharge; } }
            public void Auto() { foreach (IMyBatteryBlock obj in base.list_obj) { obj.ChargeMode = ChargeMode.Auto; } }
            public string TextInfo(string name)
            {
                StringBuilder values = new StringBuilder();
                values.Append((!String.IsNullOrWhiteSpace(name) ? name : "БАТАРЕИ") + ": [" + Count + "] [А-" + CountAuto + " З-" + CountCharger + "]" + PText.GetCurrentOfMax(CurrentPower, MaxPower, "MW") + "\n");
                values.Append("|- ЗАР:  " + PText.GetScalePersent(CurrentPower / MaxPower, 20) + "\n");
                return values.ToString();
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
        public class Upr
        {
            int cur_ref = 1;
            int max_ref = refs.list_obj.Count();
            public Upr() { }
            public string GetNameOfTemplate(string str, string tmp)
            {
                int istart = str.IndexOf("[" + tmp);
                string result = null;
                if (istart > 0)
                {
                    for (var i = istart; i < str.Length; i++)
                    {
                        result += str[i];
                        if (str[i] == ']') return result;
                    }
                }
                return result;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "ref_n": cur_ref = (cur_ref < max_ref ? cur_ref + 1 : 1); break;
                    case "ref_p": cur_ref = (cur_ref > 1 ? cur_ref - 1 : max_ref); break;
                    default: break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    foreach (IMyRefinery rf in refs.list_obj)
                    {
                        string tag = GetNameOfTemplate(rf.CustomName, "ref");
                        List<IMyLightingBlock> led_ref = new List<IMyLightingBlock>();
                        led_ref = leds.list_obj.Where(l => l.CustomName.Contains(tag)).ToList();
                        //StringBuilder values = new StringBuilder();
                        //values.Append(led_ref != null ? led_ref.Count().ToString() : "null");
                        //values.Append(tag);
                        //lcd_debug.OutText(values);
                        foreach (IMyLightingBlock led in led_ref)
                        {
                            if (rf.IsWorking)
                            {
                                if (rf.IsProducing)
                                {
                                    if (rf.IsQueueEmpty)
                                    {
                                        led.Color = Color.Green;
                                    }
                                    else
                                    {
                                        led.Color = Color.Yellow;

                                    }
                                }
                                else
                                {
                                    led.Color = Color.Blue;
                                }
                            }
                            else
                            {
                                led.Color = Color.Red;
                            }
                        }
                    }
                }
                StringBuilder values_ref = new StringBuilder();
                refs.TextInfo(tag_ref + cur_ref.ToString());
                values_ref.Append(refs.TextInfo(tag_ref + cur_ref.ToString()));
                lcd_refinery.OutText(values_ref);
            }
        }
    }
}
