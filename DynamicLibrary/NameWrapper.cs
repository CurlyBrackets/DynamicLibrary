using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    static class Names
    {
        private const string BinaryScopeResolutionOperator = "::";

        public static string Field(FieldInfo f)
        {
            return f.DeclaringType.FullName + BinaryScopeResolutionOperator + f.Name;
        }

        public static string ReverseField(string name)
        {
            return name.Substring(name.IndexOf(BinaryScopeResolutionOperator) + 2);
        }

        public static string Method(MethodBase mi)
        {
            var sb = new StringBuilder();

            sb.Append(mi.DeclaringType.FullName);
            sb.Append(BinaryScopeResolutionOperator);
            sb.Append(mi.Name);
            sb.Append("(");

            var ps = mi.GetParameters();
            for(int i=0;i<ps.Length;i++)
            {
                sb.Append(ps[i].ParameterType.FullName);
                if (i < ps.Length - 1)
                    sb.Append(", ");                
            }

            sb.Append(")");

            return sb.ToString();
        }

        public static IList<string> ReverseMethod(string name)
        {
            var ret = new List<string>();

            int i1 = name.IndexOf(BinaryScopeResolutionOperator)+2, i2 = name.IndexOf("(", i1);
            ret.Add(name.Substring(i1, i2 - i1).Trim());

            bool exit = false;
            while(!exit)
            {
                i1 = i2 + 1;
                i2 = name.IndexOf(",", i1);
                if (i2 == -1)
                {
                    i2 = name.IndexOf(")", i1);
                    if (i2 == -1)
                        break;
                    else
                        exit = true;
                }

                var item = name.Substring(i1, i2 - i1).Trim();
                if(item.Length > 0)
                    ret.Add(item);
            }

            return ret;
        }
    }
}
