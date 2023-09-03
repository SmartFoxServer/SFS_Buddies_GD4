using Godot;
using System;

using Sfs2X.Entities.Managers;
using Sfs2X.Entities.Variables;
using System.Reflection;
using System.Xml.Linq;

public partial class UserProfile : Control
{
    //   protected lobby lb;

    [Export] public Control LobbyManagerNode;
    public String callbackName;

    [ExportCategory("UI Settings")]



    [Export] public LineEdit usernameLabel;
    [Export] public CheckBox onlineToggle;
    [Export] public LineEdit nickInput;
    [Export] public LineEdit yearInput;
    [Export] public TextEdit moodInput;
    [Export] public OptionButton stateDropdown;

      public override void _Ready()
    {
      
    }
        /**
         * Show the generic user profile details.
         */
        public void InitUserProfile(string username)
    {
        // Username
        usernameLabel.Text = "Username: " + username;
    }

    /**
	 * Show the profile details related to the buddy list system.
	 */
    public void InitBuddyProfile(IBuddyManager buddyManager)
    {
        // User online/offline state
        onlineToggle.ButtonPressed = buddyManager.MyOnlineState;
        onlineToggle.FocusExited += () => { OnOnlineToggleChange(); };


        // User nickname
        nickInput.Text = (buddyManager.MyNickName != null ? buddyManager.MyNickName : "");
        nickInput.FocusExited += () => { OnNickInputEnd(); };

        // Available states and current user state
        stateDropdown.Select(buddyManager.BuddyStates.IndexOf(buddyManager.MyState));
        stateDropdown.FocusExited += () => { OnStateDropdownChange(); };
        // Buddy variable: user birth year
        BuddyVariable year = buddyManager.GetMyVariable(LobbyManager.BUDDYVAR_YEAR);
        yearInput.Text = ((year != null && !year.IsNull()) ? year.GetIntValue().ToString() : "");
        yearInput.FocusExited += () => { OnBirthYearInputEnd(); };

        // Buddy variable: user mood
        BuddyVariable mood = buddyManager.GetMyVariable(LobbyManager.BUDDYVAR_MOOD);
        moodInput.Text = ((mood != null && !mood.IsNull()) ? mood.GetStringValue() : "");
        moodInput.FocusExited += () => { OnMoodInputEnd(); };

    }

    /**
	 * Dispatch a custom event to set the user's online/offline state in the buddy list system.
	 */
    public void OnOnlineToggleChange()
    {
        if (onlineToggle.ButtonPressed)
            LobbyManagerNode.Call("OnOnlineToggleChange", true);
       else
            LobbyManagerNode.Call("OnOnlineToggleChange", false);

    }

    /**
	 * Dispatch a custom event to set the user's nickname in the buddy list system.
	 */
    public void OnNickInputEnd()
    {
        LobbyManagerNode.Call("OnBuddyNickChange", nickInput.Text);
    }

    /**
	 * Dispatch a custom event to set the user's birth year in the buddy list system.
	 */
    public void OnBirthYearInputEnd()
    {

        bool isNumeric = int.TryParse(yearInput.Text, out _);
        if (isNumeric)
            LobbyManagerNode.Call("OnBuddyYearChange", yearInput.Text);
  
    }

    /**
	 * Dispatch a custom event to set the user's mood in the buddy list system.
	 */
    public void OnMoodInputEnd()
    {
        LobbyManagerNode.Call("OnBuddyMoodChange", moodInput.Text);
    }

    /**
	 * Dispatch a custom event to set the user's state in the buddy list system.--
	 */
    public void OnStateDropdownChange()
    {

        string selectedText = stateDropdown.GetItemText(stateDropdown.Selected);
        LobbyManagerNode.Call("OnBuddyStateChange", selectedText);
    }
}
