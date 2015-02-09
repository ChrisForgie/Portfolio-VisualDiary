using UnityEngine;
using System.Collections;

/* Visual Diary
 * Class written by Chris Forgie
 * October/November 2014

/* This class is used for determing if a menu item has been selected
 * and moving between the screens accordingly.
 * It also changes the sprites of some options if they have a darker
 * being pressed animation */

public class MenuSelect : MonoBehaviour {

	//Variables required for the Main Menu screen
	public Sprite calendarClick, todayClick, reviewClick, backArrowClick, addEventClick; //These are darker icons (used for clicking animation)
	private enum itemSelections {CalendarOption = 1, TodayOption, ReviewOption, BackArrow, AddEvent, CalendarDate, SavedImage}; 
	
	private GameObject currScreen, prevScreen; //Utilised in screen switching
	private bool screenSlideProgress; //Used to make sure another screen doesnt begin when one is already in progress
	private float prevZoffSet; //The amount to push the previous screen back to make room for the new screen to slide on

	private GameObject hitObject; //Object user has clicked on
	private Sprite oldLook; //The look of a current sprite before a click animation has been pressed
	private bool spriteChanged; //Has the sprite changed, change it back when the mouse has unclicked (OnMouseUp within Update())

	private string eventDate; //The date for the add event screen - if coming from calendar then a different day must be shown

	/* This Start function is used to set the intial previous and current screens for screen switching
	 * It also defines a z-offset to move the previous screen by */
	void Start () {

		/* On startup, set the current screen to the main menu screen (first one the user sees)
		 * To avoid any glitches, set the previous screen to the main menu as well */
		currScreen = GameObject.Find("MainMenuScreen");
		prevScreen = currScreen;

		/* Create a z-offset for moving the screens. Since the camera is orthographic, the z-axis is negligible
		 * When a new screen moves on top of an old screen, we don't want any merge of objects so the offset
		 * will be used to push the previous screen back */
		prevZoffSet = 50.0f;

	}
	
	/* The update method is called once per frame, all this method will do is check if any event has
	 * taken place and update accordingly. */
	void Update () {

		checkEventChanges();
	}


	/* This method is called once per frame from the Update method within this class
	 * This method has two primary tasks.
	 * 1. Check for a left click press, determine if an interable object has been clicked?
	 * 		i.e. a new menu or a calendar date option. Then do the respective task of switching
	 * 		to the appropiate screen. If the object clicked has a pressed texture (animation)
	 * 		then switch it.
	 * 2. Check for a left click release - This is used to change a texture back to its original
	 * 		state. This only applies for objects which have a darker pressed texture and are only 
	 * 		changed when the mouse is pressed down (within this function)
	 * */
	void checkEventChanges(){

		//First task - check for left mouse click
		if(Input.GetMouseButtonDown(0)){
			
			//Create a raycast from the current mouse position
			Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			RaycastHit2D hitItem = Physics2D.Raycast(mousePos, Vector2.zero);

			if(hitItem.collider != null){
				
				/* Assign the item clicked with a number and compare with the menuSelections enum
				 * to determine which task has to be done */
				hitObject = hitItem.collider.gameObject;
				int objectPressed = 0;
				spriteChanged = true; //Set to true for default, will be set to false if no item is clicked that has a sprite that changes
				
				if(hitObject.tag == "calendarOption"){
					objectPressed = 1;
				}else if(hitObject.tag == "todayOption"){
					objectPressed = 2;
				}else if(hitObject.tag == "reviewOption"){
					objectPressed = 3;
				}else if(hitObject.tag == "backArrow"){
					objectPressed = 4;
				}else if(hitObject.tag == "addEvent"){
					objectPressed = 5;
				}else if(hitObject.tag == "calendarDate"){
					objectPressed = 6;
					spriteChanged = !spriteChanged; //This doesnt require a change in sprite
				}else if(hitObject.tag == "savedImage"){
					objectPressed = 7;
					spriteChanged = !spriteChanged; //No change required
				}else{
					spriteChanged = !spriteChanged; //set to false as nothing is changing
				}
				
				//The old look of the sprite, to revert back if we have clicked version
				if(spriteChanged){
					oldLook = hitObject.GetComponent<SpriteRenderer>().sprite;
				}

				//Once the object pressed has been determine, start the appropiate action
				switch(objectPressed){
				case (int)itemSelections.CalendarOption:
					hitObject.GetComponent<SpriteRenderer>().sprite = calendarClick;
					StartCoroutine(SlideScreens(GameObject.Find("CalendarScreen"), false));
					break;
				case (int)itemSelections.TodayOption:
					hitObject.GetComponent<SpriteRenderer>().sprite = todayClick;
					eventDate = "0"; //Default for today
					StartCoroutine(SlideScreens(GameObject.Find("TodayAddEventScreen"), false));
					break;
				case (int)itemSelections.ReviewOption:
					hitObject.GetComponent<SpriteRenderer>().sprite = reviewClick;
					break;
				case (int)itemSelections.BackArrow:
					hitObject.GetComponent<SpriteRenderer>().sprite = backArrowClick;
					StartCoroutine(SlideScreens(prevScreen, true));
					break;
				case (int)itemSelections.AddEvent:
					hitObject.GetComponent<SpriteRenderer>().sprite = addEventClick;
					StartCoroutine(SlideScreens(GameObject.Find("EventScreen"), false));
					break;
				case (int)itemSelections.CalendarDate:
					eventDate = hitObject.name;
					StartCoroutine(SlideScreens(GameObject.Find("TodayAddEventScreen"), false));
					break;
				case (int)itemSelections.SavedImage:
					StartCoroutine(SlideScreens(GameObject.Find("EventViewScreen"), false));
					break;
				default:
					break;
				}
				
			}
		}
		
		//Task 2 - When the left click has been raised, revert any changed textures
		if(Input.GetMouseButtonUp(0)){
			if(spriteChanged){
				hitObject.GetComponent<SpriteRenderer>().sprite = oldLook;
				spriteChanged = !spriteChanged; //reset back to false;
			}
		}
	}


	/* This method is used to switch between two screens, it takes two parameters - the new screen to slide 
	 * onto the existing screen and a boolean for determing which way it should slide 
	 * i.e. left to right for sliding a previous screen back on */
	IEnumerator SlideScreens(GameObject newScreen, bool isGoingBack){

		/* Make sure we are not trying to move the same screen on top of itself
		and that we are currently not already in an existing screen slide */
		if((currScreen != newScreen) && (!screenSlideProgress)){

			//Set to true as process has started
			screenSlideProgress = true;
		
			//Make sure the new screen is active and any GUI elements are shown
			showNewScreenGUI(newScreen);

			/* If there are any events that are required before the new screen is switched over.
			 * They will be checked within this method */
			newScreenSpecificEvents(newScreen.name);

			//Move the current screen back to make room for the new screen - to stop items overlapping
			if(prevZoffSet < 10.0f){ 
				prevZoffSet = 50.0f;
			}
			prevZoffSet -= 0.5f; 
			currScreen.transform.position = new Vector3(0, 0, prevZoffSet);

			//Move the new screen to the left or right of the current screen depending on which way we're moving
			if(isGoingBack){
				newScreen.transform.position = new Vector3(-10f, 0, 0);
			}else{
				newScreen.transform.position = new Vector3(10f, 0, 0);
			}

			//Define start and end positions for the new screen for the lerp function
			Vector3 newScreenStartPos = newScreen.transform.position;
			Vector3 newScreenEndPos = Vector3.zero;
			float moveTime = 0.0f;

			//Slide the new screen over the top of the current screen
			while(moveTime <= 1.1f){
				newScreen.transform.position = Vector3.Lerp (newScreenStartPos, newScreenEndPos, moveTime);
				moveTime += Time.deltaTime * 2.5f;
				yield return null;
			}

			/* Switch the current screen to the now previous screen and then set the new screen to the current screen
			 * If we are on an event screen then don't make this the previous screen so that when we go back one level 
			 * we don't get caught in a loop and hence be unable to return all the way back to the main menu */
			if(currScreen.name == "EventScreen"){ 
				prevScreen = GameObject.Find ("MainMenuScreen");
			}else if(newScreen.name == "TodayAddEventScreen"){
				prevScreen = GameObject.Find ("MainMenuScreen");
			}else{
				prevScreen = currScreen;
			}
			currScreen = newScreen;

			//Screen slide progress is finished so set the progress to false
			screenSlideProgress = !screenSlideProgress;
		}

	}

	/* This method is called from the SwitchScreens method
	 * Screens when not displayed are inactive and are hidden from camera view
	 * This action was chosen as some guiText was displaying on top of other screens when 
	 * they were previous screens.
	 * 
	 * So at this stage, set the new screen to active and make sure any hidden guiText is shown */
	void showNewScreenGUI(GameObject newScreen){

		newScreen.SetActive(true);

		foreach(Transform child in newScreen.transform){
			if(child.gameObject.GetComponent<TextMesh>() != null){
				child.gameObject.GetComponent<MeshRenderer>().enabled = true;
			}
			foreach(Transform miniChild in child){
				if(miniChild.gameObject.GetComponent<TextMesh>() != null){
					miniChild.gameObject.GetComponent<MeshRenderer>().enabled = true;
				}
				
				foreach(Transform microChild in miniChild){
					if(microChild.gameObject.GetComponent<TextMesh>() != null){
						microChild.gameObject.GetComponent<MeshRenderer>().enabled = true;
					}
				}
				
			}
		}
		
		foreach(Transform child in currScreen.transform){
			if(child.gameObject.GetComponent<TextMesh>() != null){
				child.gameObject.GetComponent<MeshRenderer>().enabled = false;
			}
			foreach(Transform miniChild in child){
				if(miniChild.gameObject.GetComponent<TextMesh>() != null){
					miniChild.gameObject.GetComponent<MeshRenderer>().enabled = false;
				}
				
				foreach(Transform microChild in miniChild){
					if(microChild.gameObject.GetComponent<TextMesh>() != null){
						microChild.gameObject.GetComponent<MeshRenderer>().enabled = false;
					}
				}
			}
		}

	}


	/* This method will check for any prequiste events that have to be carried out before a new
	 * screen is switched on top of the previous one. This method is called from the SwitchScreens
	 * method of this class.
	 * 
	 * Only the string of the new screen name is passed in as that is all that is required and space
	 * is saved in not having to make a copy of the new screen each time this method is called. */
	void newScreenSpecificEvents(string newScreenName){

		/* If we are viewing a saved event, take the small image for the AddEvent Screen and enlarge it
		 * on the Event View screen */
		if(newScreenName == "EventViewScreen"){
			Texture2D sceneTexture = hitObject.renderer.material.mainTexture as Texture2D;
			GameObject sceneView = GameObject.Find ("SceneView");
			sceneView.renderer.material.mainTexture = sceneTexture;
		}
		
		/* If we are moving back from the event creation screen then reset the event if not saved
		 * If we are moving towards the event screen then pass the event number with it (tile clicked on) */
		if((currScreen.name == "EventScreen") || (newScreenName == "EventScreen")){
			EventCreate eventObj = this.GetComponent<EventCreate>();
			
			if(currScreen.name == "EventScreen"){
				eventObj.resetEvent();
			}else{
				string addEventName = hitObject.name;
				string eventNum = addEventName.Substring(addEventName.Length-1, 1);
				eventObj.setSaveName(eventNum, eventDate);
			}
		}
		
		/* If we are moving to the Add Event Screen then pass in the appropiate date */
		if(newScreenName == "TodayAddEventScreen"){
			MenuSetup menuSet = this.GetComponent<MenuSetup>();
			menuSet.updateEventDay(eventDate);
		}
	}


}
