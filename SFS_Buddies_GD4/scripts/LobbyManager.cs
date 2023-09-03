using Godot;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Logging;
using Sfs2X.Requests;
using Sfs2X.Util;
using Sfs2X.Entities;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Sfs2X.Entities.Variables;
using static System.Net.Mime.MediaTypeNames;
using Sfs2X.Requests.Buddylist;
using Sfs2X.Entities.Data;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Xml;

/**
 * Script attached to the Controller object in the scene.
 */
public partial class LobbyManager : Control
{

    public static string GAME_ROOMS_GROUP_NAME = "games";

    //----------------------------------------------------------
    // UI elements
    //----------------------------------------------------------

    [ExportCategory("Main Panel")]

    [Export]
    public Control loginPanel;
    [Export]
    public LineEdit loggedInAsLabel;
    [Export]
    public Label userStatusLabel;
    [ExportCategory("User Panel")]
    [Export]
    public Control userProfilePanel;
    public UserProfile userProfile;
    [Export]
    public LineEdit userProfileText;
    [ExportCategory("Warning Panel")]
    [Export]
    public Control warningPopup;
    [Export]
    public TextEdit warningText;
    [ExportCategory("Game List Settings")]

    [Export]
    public PackedScene gameListScene;

    [Export]
    public ScrollContainer scrollContainer;

    private GameListItem gameListItem;


    private SmartFox sfs;
    private GlobalManager global;

    private Dictionary<int, GameListItem> gameListItems;
    private Dictionary<string, Node> itemInstances = new Dictionary<string, Node>();
    private Dictionary<string, BuddyListItem> buddyListItems;
    private Dictionary<string, Node> itemInstances2 = new Dictionary<string, Node>();

    public static string BUDDYVAR_YEAR = SFSBuddyVariable.OFFLINE_PREFIX + "year";
    public static string BUDDYVAR_MOOD = "mood";

    public BuddyListItem buddyListItemPrefab;

    [ExportCategory("Buddy Chat Settings")]

    [Export] public LineEdit buddyNameInput;
    [Export]
    public PackedScene buddyListScene;

    [Export]
    public LineEdit messageInput;
    [Export]
    public RichTextLabel chatTextArea;
    [Export] public Label nameLabel;

    private string buddyName;
    private string buddyDisplayName;
    private string lastSenderName;

    private BoxContainer buddyChatPanel;
    private BoxContainer parentHBox;

    //----------------------------------------------------------
    // Callback Methods
    //----------------------------------------------------------
    #region

    public override void _Ready()
    {
        global = (GlobalManager)GetNode("/root/globalmanager");
        sfs = global.GetSfsClient();

        loggedInAsLabel.Text = "Logged in as " + sfs.MySelf.Name;
        userProfile = GetNode<UserProfile>("User Profile Panel");
        userProfile.InitUserProfile(sfs.MySelf.Name);

        // Add event listeners
        AddSmartFoxListeners();

        // Populate list of available games
        PopulateGamesList();

        // Initialize Buddy List system
        // We have to check if it was already initialized if this scene is relaoded after leaving the Game scene
        if (!sfs.BuddyManager.Inited)
            sfs.Send(new InitBuddyListRequest());
        else
            InitializeBuddyClient();



        parentHBox = GetNode<HBoxContainer>("BackGround/Main Panel/HBox");
        parentHBox.AddThemeConstantOverride("separation", 550);

        buddyChatPanel = GetNode<VBoxContainer>("BackGround/Main Panel/HBox/VBox3");
        buddyChatPanel.Hide();
        parentHBox.AddThemeConstantOverride("separation", 550);

        // Reset buddy chat history

        BuddyChatHistoryManager.Init();
    }

    public override void _Process(double delta)
    {
        // Process the SmartFox events queue
        if (sfs != null)
            sfs.ProcessEvents();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (sfs != null && sfs.IsConnected)
                sfs.Disconnect();

            GD.Print("Application Quit");
        }
    }


    #endregion

    //----------------------------------------------------------
    // UI event listeners
    //----------------------------------------------------------
    #region


    /**
	 * On Logout button click, disconnect from SmartFoxServer.
	 */
    public void OnLogoutButtonClick()
    {
        sfs.Disconnect();

    }
    /**
	 * On Start game button click, create and join a new game Room.
	 */
    public void OnStartGameButtonClick()
    {
        // Configure Room
        RoomSettings settings = new RoomSettings(sfs.MySelf.Name + "'s game");
        settings.GroupId = GAME_ROOMS_GROUP_NAME;
        settings.IsGame = true;
        settings.MaxUsers = 2;
        settings.MaxSpectators = 10;

        // Request Room creation to server
        sfs.Send(new CreateRoomRequest(settings, true));
    }

    /**
	 * On Play game button click in Game List Item prefab instance, join an existing game Room as a player.
	 */
    public void OnGameItemPlayClick(int roomId)
    {
        // Join game Room as player
        sfs.Send(new Sfs2X.Requests.JoinRoomRequest(roomId));
    }

    /**
	 * On Watch game button click in Game List Item prefab instance, join an existing game Room as a spectator.
	 */
    public void OnGameItemWatchClick(int roomId)
    {
        // Join game Room as spectator
        sfs.Send(new Sfs2X.Requests.JoinRoomRequest(roomId, null, null, true));
    }

    /**
	 * On User icon click, show User Profile Panel prefab instance.
	 */
    public void OnUserIconClick()
    {
        userProfilePanel.Position = (Vector2I)((GetViewportRect().Size - userProfilePanel.Size) / 2);
        userProfileText.Text = "Username: " + sfs.MySelf.Name;
        GetTree().Paused = true;
        GetNode<Control>("User Profile Panel").Show();

    }
    public void OnUserIconCloseClick()
    {
        GetNode<Control>("User Profile Panel").Hide();
        GetTree().Paused = false;
    }

    public void OnWarningPanelShow()
    {
        warningPopup.Position = (Vector2I)((GetViewportRect().Size - warningPopup.Size) / 2);
        GetTree().Paused = true;
        GetNode<Control>("Warning Panel").Show();
    }
    public void OnWarningPanelHide()
    {
        GetNode<Control>("Warning Panel").Hide();
        GetTree().Paused = false;
    }

    /**
	 * On buddy name input edit end, if the Enter key was pressed, send request to add buddy to user's Buddy List.
	 */
    public void OnBuddyNameInputEndEdit()
    {
        if (Input.IsActionJustPressed("ui_accept") && !Input.IsActionJustPressed("ui_select"))
            OnAddBuddyButtonClick();
    }

    /**
	 * On Add buddy button click, send request to add buddy to user's Buddy List.
	 */
    public void OnAddBuddyButtonClick()
    {
        if (buddyNameInput.Text != "")
        {
            // Request buddy adding to buddy list
            sfs.Send(new AddBuddyRequest(buddyNameInput.Text));
            buddyNameInput.Text = "";
        }
    }

    /**
      * On Add buddy button click in Buddy List Item prefab instance, send request to add temporary buddy to user's Buddy List.
      */
    public void OnAddBuddyButtonClick2(string buddyName)
    {
        sfs.Send(new AddBuddyRequest(buddyName));
    }

    /**
	 * On Block buddy button click in Buddy List Item prefab instance, send request to block/unblock buddy in user's Buddy List.
	 */
    public void OnBlockBuddyButtonClick(string buddyName)
    {
        bool isBlocked = sfs.BuddyManager.GetBuddyByName(buddyName).IsBlocked;

        // Request buddy block/unblock
        sfs.Send(new BlockBuddyRequest(buddyName, !isBlocked));
    }

    /**
	 * On Remove buddy button click in Buddy List Item prefab instance, send request to remove buddy from user's Buddy List.
	 */
    public void OnRemoveBuddyButtonClick(string buddyName)
    {
        // Request buddy removal from buddy list
        sfs.Send(new RemoveBuddyRequest(buddyName));
    }

    /**
	 * On Chat button click in Buddy List Item prefab instance, initialize and show chat panel.
	 */
    public void OnChatBuddyButtonClick(string buddyName)
    {
        // Skip if chat is already active with the same buddy
        //      if (chatPanel.BuddyName != buddyName)
        {
            // Get chat history (if any) and reset new messages counter
            List<BuddyChatMessage> buddyChatMessages = BuddyChatHistoryManager.GetBuddyChatMessages(buddyName);
            BuddyChatHistoryManager.ResetUnreadMessagesCount(buddyName);

            // Initialize and show panel
            InitChat(sfs.BuddyManager.GetBuddyByName(buddyName), buddyChatMessages);

            // Move buddy list item to top of the list and reset new messages counter
            buddyListItems.TryGetValue(buddyName, out BuddyListItem buddyListItem);

            if (buddyListItem != null)
            {
                buddyListItem.SetChatMsgCounter(0);
            }
        }
    }



    /**
     * On custom event fired by Buddy Chat Panel prefab instance, send message to buddy.
     */
    public void OnBuddyMessageSubmit(string buddyName, string message)
    {
        // Add a custom parameter containing the recipient name
        ISFSObject _params = new SFSObject();
        _params.PutUtfString("recipient", buddyName);

        // Retrieve buddy
        Buddy buddy = sfs.BuddyManager.GetBuddyByName(buddyName);

        // Send message to buddy
        sfs.Send(new BuddyMessageRequest(message, buddy, _params));
    }

    /**
	 * On custom event fired by User Profile Panel prefab instance, toggle user online state in Buddy List system.
	 */
    public void OnOnlineToggleChange(bool isChecked)
    {
        // Send request to to toggle online/offline state
        sfs.Send(new GoOnlineRequest(isChecked));
    }

    /**
	 * On custom event fired by User Profile Panel prefab instance, set user's Buddy Variables.
	 */

    public void OnBuddyNickChange(string nickInput)
    {
        string varName = ReservedBuddyVariables.BV_NICKNAME;
        object value = nickInput;

        List<BuddyVariable> buddyVars = new List<BuddyVariable>();
        buddyVars.Add(new SFSBuddyVariable(varName, value));

        // Set Buddy Variables
        sfs.Send(new SetBuddyVariablesRequest(buddyVars));
    }

    public void OnBuddyMoodChange(string moodInput)
    {
        string varName = LobbyManager.BUDDYVAR_MOOD;
        object value = moodInput;

        List<BuddyVariable> buddyVars = new List<BuddyVariable>();
        buddyVars.Add(new SFSBuddyVariable(varName, value));

        // Set Buddy Variables
        sfs.Send(new SetBuddyVariablesRequest(buddyVars));
    }

    public void OnBuddyYearChange(string yearInput)
    {
        string varName = LobbyManager.BUDDYVAR_YEAR;
        object value;
        if (yearInput != "")
        { value = Int32.Parse(yearInput); }
        else
        { value = null; }
        List<BuddyVariable> buddyVars = new List<BuddyVariable>();
        buddyVars.Add(new SFSBuddyVariable(varName, value));

        // Set Buddy Variables
        sfs.Send(new SetBuddyVariablesRequest(buddyVars));
    }
    public void OnBuddyStateChange(string state)
    {
        string varName = ReservedBuddyVariables.BV_STATE;

        List<BuddyVariable> buddyVars = new List<BuddyVariable>();
        buddyVars.Add(new SFSBuddyVariable(varName, state));

        // Set Buddy Variables
        sfs.Send(new SetBuddyVariablesRequest(buddyVars));
    }

    #endregion

    //----------------------------------------------------------
    // Helper methods
    //----------------------------------------------------------
    #region
    private void AddSmartFoxListeners()
    {

        sfs.AddEventListener(SFSEvent.ROOM_CREATION_ERROR, OnRoomCreationError);
        sfs.AddEventListener(SFSEvent.ROOM_ADD, OnRoomAdded);
        sfs.AddEventListener(SFSEvent.ROOM_REMOVE, OnRoomRemoved);
        sfs.AddEventListener(SFSEvent.USER_COUNT_CHANGE, OnUserCountChanged);
        sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
        sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);

        sfs.AddEventListener(SFSBuddyEvent.BUDDY_LIST_INIT, OnBuddyListInit);
        sfs.AddEventListener(SFSBuddyEvent.BUDDY_ERROR, OnBuddyError);
        sfs.AddEventListener(SFSBuddyEvent.BUDDY_ONLINE_STATE_UPDATE, OnBuddyOnlineStateUpdate);
        sfs.AddEventListener(SFSBuddyEvent.BUDDY_VARIABLES_UPDATE, OnBuddyVariablesUpdate);
        sfs.AddEventListener(SFSBuddyEvent.BUDDY_BLOCK, OnBuddyBlock);
        sfs.AddEventListener(SFSBuddyEvent.BUDDY_REMOVE, OnBuddyRemove);
        sfs.AddEventListener(SFSBuddyEvent.BUDDY_ADD, OnBuddyAdd);
        sfs.AddEventListener(SFSBuddyEvent.BUDDY_MESSAGE, OnBuddyMessage);
    }

    /**
	 * Remove all SmartFoxServer-related event listeners added by the scene.
	 */
    private void RemoveSmartFoxListeners()
    {
        sfs.RemoveEventListener(SFSEvent.ROOM_CREATION_ERROR, OnRoomCreationError);
        sfs.RemoveEventListener(SFSEvent.ROOM_ADD, OnRoomAdded);
        sfs.RemoveEventListener(SFSEvent.ROOM_REMOVE, OnRoomRemoved);
        sfs.RemoveEventListener(SFSEvent.USER_COUNT_CHANGE, OnUserCountChanged);
        sfs.RemoveEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
        sfs.RemoveEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);

        sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_LIST_INIT, OnBuddyListInit);
        sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_ERROR, OnBuddyError);
        sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_ONLINE_STATE_UPDATE, OnBuddyOnlineStateUpdate);
        sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_VARIABLES_UPDATE, OnBuddyVariablesUpdate);
        sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_BLOCK, OnBuddyBlock);
        sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_REMOVE, OnBuddyRemove);
        sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_ADD, OnBuddyAdd);
        sfs.RemoveEventListener(SFSBuddyEvent.BUDDY_MESSAGE, OnBuddyMessage);
    }

    private void PopulateGamesList()
    {
        // Initialize list
        if (gameListItems == null)
            gameListItems = new Dictionary<int, GameListItem>();

        // For the game list we use a scrollable area containing a separate prefab for each Game Room
        // The prefab contains clickable buttons to join the game
        List<Room> rooms = sfs.RoomManager.GetRoomList();

        // Display game list items
        foreach (Room room in rooms)
            AddGameListItem(room);

    }

    /**
 * Create Game List Item prefab instance and add to games list.
 */
    private void AddGameListItem(Room room)
    {
        // Show only game rooms
        // Also password protected Rooms are skipped, to make this example simpler
        // (protection would require an interface element to input the password)
        if (!room.IsGame || room.IsHidden || room.IsPasswordProtected)
            return;

        var instance = gameListScene.Instantiate<Control>();
        GameListItem gameListItem = instance.GetNode<GameListItem>("GameListItem");

        // Init game list item
        gameListItem.Init(room);
        gameListItems.Add(room.Id, gameListItem);


        // Connect method to play and watch buttons
        gameListItem.playButton.Pressed += () => { OnGameItemPlayClick(room.Id); };
        gameListItem.watchButton.Pressed += () => { OnGameItemWatchClick(room.Id); };

        var vboxContainer = GetNode<VBoxContainer>("BackGround/Main Panel/HBox/VBox1/Active Games/ScrollContainer/VBoxContainer");
        vboxContainer.AddChild(instance);
        itemInstances.Add(room.Id.ToString(), instance);
    }

    /**
	 * Show current user state in Buddy List system.
	 */
    private void DisplayUserStateAsBuddy()
    {
        if (sfs.BuddyManager.MyOnlineState)
            userStatusLabel.Text = sfs.BuddyManager.MyState;
        else
            userStatusLabel.Text = "Offline";
    }

    /**
	 * Initialize buddy-related entities.
	 */
    private void InitializeBuddyClient()
    {
        // Init buddy-related data structures
        buddyListItems = new Dictionary<string, BuddyListItem>();

        // For the buddy list we use a scrollable area containing a separate prefab for each buddy
        // The prefab contains clickable buttons to chat with the buddy and block or remove them
        List<Buddy> buddies = sfs.BuddyManager.BuddyList;

        // Display buddy list items
        // All blocked buddies are displayed at the bottom of the list
        foreach (Buddy buddy in buddies)
            AddBuddyListItem(buddy, !buddy.IsBlocked);

        // Set current user details in buddy system
        userProfile = GetNode<UserProfile>("User Profile Panel");
        userProfile.InitBuddyProfile(sfs.BuddyManager);

        // Display user state in buddy list system
        DisplayUserStateAsBuddy();

        // Show/hide buddy list
        if (sfs.BuddyManager.MyOnlineState)
        {
            GetNode<Control>("BackGround/Main Panel/HBox/VBox2").Show();
        }
        else
        {
            GetNode<Control>("BackGround/Main Panel/HBox/VBox2").Hide();

        }

    }

    /**
	 * Create Buddy List Item prefab instance and add to buddy list.
	 */
    private void AddBuddyListItem(Buddy buddy, bool toTop = false)
    {
        string buddyName = buddy.Name;

        // Check if buddy list item already exist
        // This could happen if a temporary buddy is added permanently
        if (buddyListItems.ContainsKey(buddyName))
        {
            BuddyListItem buddyListItem = buddyListItems[buddyName];
            buddyListItem.SetState(buddy);
        }
        else
        {
            var instance = buddyListScene.Instantiate<Control>();
            BuddyListItem buddyListItem = instance.GetNode<BuddyListItem>("BuddyListItem");

            // Init buddy list item
            buddyListItem.Init(buddy);

            // Set unread messages counter
            // (buddy could have sent messages while user was in the game scene)

            buddyListItem.SetChatMsgCounter(BuddyChatHistoryManager.GetUnreadMessagesCount(buddy.Name));

            // Connect method to play and watch buttons
            buddyListItem.removeButton.Pressed += () => { OnRemoveBuddyButtonClick(buddyName); };
            buddyListItem.addButton.Pressed += () => { OnAddBuddyButtonClick2(buddyName); };
            buddyListItem.blockButton.Pressed += () => { OnBlockBuddyButtonClick(buddyName); };
            buddyListItem.chatButton.Pressed += () => { OnChatBuddyButtonClick(buddyName); };


            var vboxContainer = GetNode<VBoxContainer>("BackGround/Main Panel/HBox/VBox2/Buddies/ScrollContainer/VBoxContainer");
            vboxContainer.AddChild(instance);
            itemInstances2.Add(buddyName.ToString(), instance);
            buddyListItems.Add(buddyName, buddyListItem);



        }
    }

    /**
	 * Update Buddy List Item prefab instance when buddy state changes.
	 */
    private void UpdateBuddyListItem(Buddy buddy)
    {
        // Get reference to buddy list item corresponding to Buddy
        buddyListItems.TryGetValue(buddy.Name, out BuddyListItem buddyListItem);

        if (buddyListItem != null)
        {

            // Update buddy list item
            buddyListItem.SetState(buddy);

        }

    }

    #endregion

    //----------------------------------------------------------
    // SmartFoxServer event listeners
    //----------------------------------------------------------
    #region
    private void OnRoomCreationError(BaseEvent evt)
    {
        // Show Warning Panel prefab instance
        warningText.Text = ("Room creation failed: " + (string)evt.Params["errorMessage"]);
        OnWarningPanelShow();
    }

    private void OnRoomAdded(BaseEvent evt)
    {
        Room room = (Room)evt.Params["room"];

        // Display game list item
        AddGameListItem(room);
    }

    public void OnRoomRemoved(BaseEvent evt)
    {
        Room room = (Room)evt.Params["room"];

        // Get reference to game list item corresponding to Room
        gameListItems.TryGetValue(room.Id, out GameListItem gameListItem);
        // Remove game list item
        if (gameListItem != null)
        {

            var vboxContainer = GetNode<VBoxContainer>("BackGround/Main Panel/HBox/VBox1/Active Games/ScrollContainer/VBoxContainer");
            vboxContainer.RemoveChild(itemInstances[room.Id.ToString()]);
            itemInstances.Remove(room.Id.ToString());

            // Remove game list item from dictionary
            gameListItems.Remove(room.Id);
            gameListItem.QueueFree();

        }

    }

    public void OnUserCountChanged(BaseEvent evt)
    {
        Room room = (Room)evt.Params["room"];

        // Get reference to game list item corresponding to Room
        gameListItems.TryGetValue(room.Id, out GameListItem gameListItem);

        // Update game list item
        if (gameListItem != null)
            gameListItem.SetState(room);

    }

    private void OnRoomJoin(BaseEvent evt)
    {
        // Set user as "Away" in Buddy List system
        if (sfs.BuddyManager.MyOnlineState)
            sfs.Send(new SetBuddyVariablesRequest(new List<BuddyVariable> { new SFSBuddyVariable(ReservedBuddyVariables.BV_STATE, "Away") }));

        // Load game scene
        RemoveSmartFoxListeners();
        GetTree().ChangeSceneToFile("game.tscn");
    }

    private void OnRoomJoinError(BaseEvent evt)
    {
        // Show Warning Panel prefab instance
        warningText.Text = ("Room join failed: " + (string)evt.Params["errorMessage"]);
        OnWarningPanelShow();
    }
    private void OnBuddyListInit(BaseEvent evt)
    {
        // Initialize buddy-related entities
        InitializeBuddyClient();
    }

    private void OnBuddyError(BaseEvent evt)
    {
        // Show Warning Panel prefab instance
        warningText.Text = ("Buddy list system error: " + (string)evt.Params["errorMessage"]);
        OnWarningPanelShow();
    }

    public void OnBuddyOnlineStateUpdate(BaseEvent evt)
    {
        Buddy buddy = (Buddy)evt.Params["buddy"];
        bool isItMe = (bool)evt.Params["isItMe"];
        var buddiesBox = GetNode<VBoxContainer>("BackGround/Main Panel/HBox/VBox2");

        // As this event is fired in case of online state update for both the current user and their buddies,
        // we have to check who the event refers to

        if (!isItMe)
            UpdateBuddyListItem(buddy);
        else
        {
            DisplayUserStateAsBuddy();

            // Show/hide buddy list

            if (sfs.BuddyManager.MyOnlineState)
                buddiesBox.Show();

            if (!sfs.BuddyManager.MyOnlineState)
            {
                buddiesBox.Hide();
                // Hide chat if current user went offline
                buddyChatPanel.Hide();
            }
            else
            {
                // Update all buddy items if current user came online
                foreach (Buddy b in sfs.BuddyManager.BuddyList)
                    UpdateBuddyListItem(b);
            }
        }
    }

    public void OnBuddyVariablesUpdate(BaseEvent evt)
    {
        Buddy buddy = (Buddy)evt.Params["buddy"];
        bool isItMe = (bool)evt.Params["isItMe"];

        // As this event is fired in case of Buddy Variables update for both the current user and their buddies,
        // we have to check who the event refers to

        if (!isItMe)
            UpdateBuddyListItem(buddy);
        else
            DisplayUserStateAsBuddy();
    }

    public void OnBuddyBlock(BaseEvent evt)
    {
        Buddy buddy = (Buddy)evt.Params["buddy"];

        UpdateBuddyListItem(buddy);
    }

    public void OnBuddyRemove(BaseEvent evt)
    {
        Buddy buddy = (Buddy)evt.Params["buddy"];

        // Get reference to buddy list item corresponding to Buddy
        buddyListItems.TryGetValue(buddy.Name, out BuddyListItem buddyListItem);

        // Remove buddy list item
        if (buddyListItem != null)
        {

            // Remove game list item

            var vboxContainer = GetNode<VBoxContainer>("BackGround/Main Panel/HBox/VBox2/Buddies/ScrollContainer/VBoxContainer");
            vboxContainer.RemoveChild(itemInstances2[buddy.Name.ToString()]);
            itemInstances2.Remove(buddy.Name.ToString());

            // Remove buddy list item from dictionary
            buddyListItems.Remove(buddy.Name);
            buddyListItem.QueueFree();

            // Block chat interaction
            if (BuddyName == buddy.Name)
            {
                buddyChatPanel.Hide();
                parentHBox.AddThemeConstantOverride("separation", 550);

            }

        }
    }

    public void OnBuddyAdd(BaseEvent evt)
    {
        Buddy buddy = (Buddy)evt.Params["buddy"];

        // Add buddy list item at the top of the list
        AddBuddyListItem(buddy, true);
    }

    public void OnBuddyMessage(BaseEvent evt)
    {
        // Add message to queue
        BuddyChatMessage chatMsg = BuddyChatHistoryManager.AddMessage(evt.Params);

        // Show message or increase message counter
        if (BuddyName == chatMsg.buddyName)
        {
            // Display message in chat panel
            PrintChatMessage(chatMsg);
        }
        else
        {
            // Increase unread messages count
            // NOTE: there's no need to make sure the sender is not the current user,
            // as in this example the current user can't send buddy messages in other ways than
            // the chat panel (which would make the above if statement true)
            int unreadMsgCnt = BuddyChatHistoryManager.IncreaseUnreadMessagesCount(chatMsg.buddyName);

            // Update buddy list item
            BuddyListItem buddyListItem = buddyListItems[chatMsg.buddyName];
            buddyListItem.SetChatMsgCounter(unreadMsgCnt);
        }

    }

    public void InitChat(Buddy buddy, List<BuddyChatMessage> history)
    {
        buddyName = buddy.Name;

        // Configure chat panel
        SetState(buddy);

        // Clear previous chat messages (panel is reused for different buddies)
        chatTextArea.Text = "";

        // Print chat history
        if (history != null)
            foreach (BuddyChatMessage messageObj in history)
                PrintChatMessage(messageObj);

        // Show chat panel
        buddyChatPanel.Show();
        parentHBox.AddThemeConstantOverride("separation", 400);


    }
    /**
  * Update prefab instance based on the state of the buddy the current user is chatting with.
  */
    public void SetState(Buddy buddy)
    {
        // Nickname
        string newBuddyDisplayName = (buddy.NickName != null && buddy.NickName != "") ? buddy.NickName : buddy.Name;

        if (newBuddyDisplayName != buddyDisplayName)
        {
            PrintSystemMessage(buddyDisplayName + " is now known as " + newBuddyDisplayName);
            buddyDisplayName = newBuddyDisplayName;
        }

        nameLabel.Text = "with\n" + buddyDisplayName;

        // Enable/disable message input
        // SetInputInteractable(buddy.IsOnline && !buddy.IsBlocked);
    }

    /**
  * Display a chat message.
  */
    public void PrintChatMessage(BuddyChatMessage messageObj)
    {
        string senderName = messageObj.sentByMe ? "" : buddyDisplayName;

        if (senderName != lastSenderName)
            chatTextArea.Text += "[b]" + (senderName == "" ? "Me" : senderName) + "[/b]\n";

        // Print chat message
        chatTextArea.Text += messageObj.message + "\n";

        // Save reference to last message sender, to avoid repeating the name for subsequent messages from the same sender
        lastSenderName = senderName;


    }
    /**
  * Display a system message.
  */
    private void PrintSystemMessage(string message)
    {
        // Print message
        chatTextArea.Text += "[color=white][i]" + message + "[/i][/color]\n";
    }

    /**
   * Return the name of the buddy with whom the current user is chatting.
   */
    public string BuddyName
    {
        get => buddyName;
    }


    /**
     * On Close button click, hide panel instance.
     */
    public void OnCloseButtonClick()
    {
        buddyChatPanel.Hide();
        parentHBox.AddThemeConstantOverride("separation", 550);

    }

    /**
     * On chat message input edit end, if Enter key was pressed, send message to buddy.
     */
    public void OnMessageInputEndEdit()
    {
        GD.Print("OnMessageInputEndEdit");
        if (Input.IsActionJustPressed("ui_accept") && !Input.IsActionJustPressed("ui_select"))
            OnSendMessageButtonClick();
    }

    /**
     * On Send button click, send message to buddy.
     */
    public void OnSendMessageButtonClick()
    {
        GD.Print("OnSendMessageButtonClick");
        if (messageInput.Text != "")
        {
            GD.Print(messageInput.Text);
            // Dispatch event
            OnBuddyMessageSubmit(buddyName, messageInput.Text);
            // Reset input
            messageInput.Text = "";
            messageInput.Select();
        }
    }


    /**
     * Set focus on message input.
     */
    private void SetInputFocus()
    {
        messageInput.Select();

    }
    #endregion
}