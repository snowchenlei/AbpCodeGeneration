using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AbpCodeGeneration.VisualStudio.Common
{
    public class EnumHelper
    {
        /// <summary>
        /// 获取特性
        /// </summary>
        /// <typeparam name="T">Attribute类型</typeparam>
        /// <param name="enumValue"></param>
        /// <returns>特性</returns>
        public static T GetAttribute<T>(Type enumValue) where T : Attribute
        {
            Type type = enumValue.GetType();
            MemberInfo[] memInfo = type.GetMember(enumValue.ToString());
            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(T), false);
                if (attrs != null && attrs.Length > 0)
                {
                    return (T)attrs[0];
                }
            }
            return default(T);
        }
        /// <summary>
        /// 枚举转字典集合
        /// </summary>
        /// <typeparam name="T">枚举类名称</typeparam>
        /// <param name="keyDefault">默认key值</param>
        /// <param name="valueDefault">默认value值</param>
        /// <returns>返回生成的字典集合</returns>
        public static Dictionary<int, string> EnumToDictionary<T>(int? keyDefault, string valueDefault)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            Type enumType = typeof(T);
            if (!enumType.IsEnum)
            {
                return result;
            }
            if (keyDefault.HasValue) //判断是否添加默认选项
            {
                result.Add(keyDefault.Value, valueDefault);
            }
            string[] fieldstrs = Enum.GetNames(enumType); //获取枚举字段数组
            foreach (var item in fieldstrs)
            {
                string description = string.Empty;
                var field = enumType.GetField(item);
                object[] arr = field.GetCustomAttributes(typeof(DescriptionAttribute), true); //获取属性字段数组
                if (arr != null && arr.Length > 0)
                {
                    description = ((DescriptionAttribute)arr[0]).Description;   //属性描述
                }
                else
                {
                    description = item;  //描述不存在取字段名称
                }
                result.Add((int)Enum.Parse(enumType, item), description);  //不用枚举的value值作为字典key值的原因从枚举例子能看出来，其实这边应该判断他的值不存在，默认取字段名称
            }
            return result;
        }
    }
}
