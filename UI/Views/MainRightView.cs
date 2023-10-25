using CP_SDK.XUI;
using System.Reflection;

namespace CP_SDK.UI.Views
{
    /// <summary>
    /// Welcome Right View controller
    /// </summary>
    public sealed class MainRightView : ViewController<MainRightView>
    {
        /// <summary>
        /// On view creation
        /// </summary>
        protected override sealed void OnViewCreation()
        {
            var l_Bytes  = Misc.Resources.FromRelPath(Assembly.GetExecutingAssembly(), "CP_SDK._Resources.ChatPlexLogoTransparent.png");
            var l_Sprite = Unity.SpriteU.CreateFromRaw(l_Bytes);

            Templates.FullRectLayout(
                Templates.TitleBar("Powered By"),

                XUIPrimaryButton.Make("")
                    .SetBackgroundSprite(null)
                    .SetIconSprite(l_Sprite)
                    .SetWidth(52)
                    .SetHeight(52)
            )
            .SetBackground(true, null, true)
            .BuildUI(transform);
        }
    }
}
