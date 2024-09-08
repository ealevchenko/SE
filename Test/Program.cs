using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    
    class Program
    {
        static public string GetNameOfTemplate(string str, string tmp)
        {
            int istart = str.IndexOf(tmp);
            string result = null;
            if (istart >= 0)
            {
                for (var i = istart; i < str.Length; i++)
                {
                    result += str[i];
                    if (str[i] == ']') return result;
                }
            }
            return result;
        }


        public class MyComp
        {
            public Component component { get; set; }
            public int value { get; set; }
        }
        public enum Component : int
        {
            BulletproofGlass = 0,
            Computer = 1,
            Construction = 2,
            Detector = 3,
            Display = 4,
            Girder = 5,
            InteriorPlate = 6,
            LargeTube = 7,
            MetalGrid = 8,
            Motor = 9,
            PowerCell = 10,
            RadioCommunication = 11,
            SmallTube = 12,
            SteelPlate = 13,
            Superconductor = 14,
        };
        static public string SetListComponent(string list, List<MyComp> components)
        {

            string[] list_st = list.Split('\n');
            // Пройдемся по помещениям и настроим панели
            foreach (Component com in Enum.GetValues(typeof(Component)))
            {
                int value = 0;
                MyComp mycom = components.Where(c => c.component == com).FirstOrDefault();
                if (mycom != null)
                {
                    value = mycom.value;
                }
                int index = Array.FindIndex(list_st, element => element.Contains(com.ToString()));
                if (index > 0)
                {
                    int indexOfChar = list_st[index].IndexOf('='); //
                    list_st[index] = list_st[index].Substring(0, indexOfChar + 1) + value.ToString();
                }
            }
            string result = "";
            foreach (string st in list_st) {
                result += st + "\n";
            }
            return result;
        }
        static void Main(string[] args)
        {
            string str = GetNameOfTemplate("dr [dr-gateway-01] [rm-angar_tech]- door", "[dr-gateway-");
            Console.WriteLine(str);




            ////            string st = "\n" +
            ////"\n" +
            ////"Positive number: stores wanted amount, removes excess (e.g.: 100)\n" +
            ////"Negative number: doesn't store items, only removes excess (e.g.: -100)\n" +
            ////"Keyword 'all': stores all items of that subtype (like a type container)\n" +
            ////"\n" +
            ////"Component/BulletproofGlass=0\n" +
            ////"Component/Computer=0\n" +
            ////"Component/Construction=0\n" +
            ////"Component/Detector=0\n" +
            ////"Component/Display=0\n" +
            ////"Component/Girder=0\n" +
            ////"Component/InteriorPlate=0\n" +
            ////"Component/LargeTube=0\n" +
            ////"Component/MetalGrid=0\n" +
            ////"Component/Motor=12\n" +
            ////"Component/PowerCell=0\n" +
            ////"Component/RadioCommunication=0\n" +
            ////"Component/SmallTube=0\n" +
            ////"Component/SteelPlate=0\n" +
            ////"Component/Superconductor=0\n" +
            ////"Ore/Cobalt=0\n" +
            ////"Ore/Ice=0\n";

            //StringBuilder test_info = new StringBuilder();
            //test_info.Append(st);


            ////List<MyComp> list1 = new List<MyComp>();
            ////list1.Add(new MyComp() { component = Component.Display, value = 1000 });
            ////list1.Add(new MyComp() { component = Component.Motor, value = 5000 });

            ////string res = SetListComponent(st, list1);

            ////Console.WriteLine(res);



            //string[] str = st.Split('\n');

            //int value = 10;

            //int index = Array.FindIndex(str, element => element.Contains("Motor"));
            //if (index > 0)
            //{
            //    int indexOfChar = str[index].IndexOf('='); // равно 4
            //    str[index] = str[index].Substring(0, indexOfChar + 1) + value.ToString();

            //}

            //foreach (string s in str)
            //{
            //    Console.WriteLine(s);
            //}


        }
    }
}
