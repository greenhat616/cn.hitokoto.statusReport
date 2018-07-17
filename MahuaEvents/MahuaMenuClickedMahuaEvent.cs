using Newbe.Mahua.MahuaEvents;
using System;

namespace Newbe.Mahua.Plugins.Template.MPQ1.MahuaEvents {
    /// <summary>
    /// 菜单点击事件
    /// </summary>
    public class MahuaMenuClickedMahuaEvent
        : IMahuaMenuClickedMahuaEvent {
        private readonly IMahuaApi _mahuaApi;
        private settingForm settingForm;

        public MahuaMenuClickedMahuaEvent(
            IMahuaApi mahuaApi) {
            _mahuaApi = mahuaApi;
        }

        public void ProcessManhuaMenuClicked(MahuaMenuClickedContext context) {
            if (context.Menu.Id == "settingMenu") {
                showSettingForm();
            }
            // todo 填充处理逻辑
            // throw new NotImplementedException();
            // 不要忘记在MahuaModule中注册
        }

        public void showSettingForm () {
            if (settingForm == null || settingForm.IsDisposed) {
                settingForm = new settingForm();
                settingForm.Show();
            } else {
                settingForm.WindowState = System.Windows.Forms.FormWindowState.Normal;
                settingForm.Activate();
                settingForm.Show();
            }
        }
    }
}
