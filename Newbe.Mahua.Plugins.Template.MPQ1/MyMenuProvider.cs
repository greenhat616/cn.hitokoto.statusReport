using System.Collections.Generic;

namespace Newbe.Mahua.Plugins.Template.MPQ1
{
    public class MyMenuProvider : IMahuaMenuProvider
    {
        public IEnumerable<MahuaMenu> GetMenus()
        {
            return new[]
            {
                new MahuaMenu
                {
                    Id = "settingMenu",
                    Text = "配置中心"
                }
            };
        }
    }
}
