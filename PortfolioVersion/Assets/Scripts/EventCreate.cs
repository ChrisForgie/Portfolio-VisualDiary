using UnityEngine;
using System;
using System.Collections;
using System.IO;

/* Visual Diary
 * Class written by Chris Forgie
 * October/November 2014

/* This class is used for creating an event
 * It will allow the user to cycle through a series of background
 * images, the scene will ask the user a few questions such as "who was
 * there?" while sliding in suitable drag&drop characters to the scene.
 * Once the user has answered all the questions, the scene will be saved
 * to an external image in the Resources folder with a unique name to identify
 * it. These images will be loaded as textures in the TodayAddEventScreen within
 * the MenuSetup class and can be displayed again by the user but not edited */

public class EventCreate : MonoBehaviour {

	//Variables required for the Event Screen
	public Sprite switchImageArrow, questionTick; //The darker (clicked) icons for the event creation screen
	private enum itemSelections {BackOption = 1, ForwardOption, QuestionAnswer};
	private GameObject backArrow, forwardArrow; 

	private GameObject hitObject; //Item the user has clicked on
	private Sprite oldLook; //The look of a current sprite before a click has been pressed
	private bool spriteChanged; //Has the sprite changed, change it back when the mouse has unclicked

	private GameObject backgroundImages; //The gameobject with all the event backgrounds grouped as one
	private int currentImage;
	private bool screenSlideProgress;

	private int questionStage; //Which stage of question are we at? The user choosing background, characters or items
	private string question1, question2, question3;
	private GameObject headerBar; //The bar at the top of this screen which will house the characters and items

	public GameObject[] charPrefabs; //The prefabs for all the characters - arranged in inspector
	public GameObject[] objPrefabs; 
	private GameObject clickedObj; 
	private bool objDragging; 

	private bool sceneSaved; //Have we saved the scene to an image
	private string saveName; //The file name given to the scene when saved



	/* The following method is called from MenuSelect to make sure that when switching screens
	 * everything gets reset so when if we go back to this screen again, it's back to the default settings */
	public void resetEvent(){

		//set the current image back to the start
		currentImage = 0;

		//Change the background image position to fit where the screen is moving to in a screen slide
		backgroundImages.transform.position = new Vector3(0, 0, 0);

		//Reset the forward arrow too if we end at the last image and go back a screen
		forwardArrow.renderer.enabled = true;
		forwardArrow.collider2D.enabled = true;

		//Also reset the question position
		questionStage = 1;
		GameObject questionTag = GameObject.Find ("questionTag");
		questionTag.GetComponentInChildren<TextMesh>().text = question1;
		GameObject questionAnswer = GameObject.Find ("questionAnswer");
		questionAnswer.renderer.enabled = true;

		//Slide characters and objects out of view
		StartCoroutine(SlideCharacters(false));
		StartCoroutine(SlideObjects(false));

		clickedObj = null;
		objDragging = false;

		//Empty the created scene
		GameObject createdScene = GameObject.Find ("createdScene");
		int numChildren = createdScene.transform.childCount;
		for(int i = numChildren - 1; i >= 0; i--){
			GameObject.Destroy(createdScene.transform.GetChild(i).gameObject);
		}

		//Reset scene saved
		sceneSaved = false;

	}


	/* This method is called from the MenuSelect SlideScreens() method, it passes in which event has been clicked 
	 * on (1-6) so we can give this file an appropiate save name.
	 * dateSave is a different day if selecting a day from the calendar rather than today */
	public void setSaveName(string eventNum, string dateSave){

		//Structure a file name using the date passed and an event number
		DateTime now;
		
		if(dateSave == "0"){
			now = DateTime.Now;
		}else{
			int year = Convert.ToInt16(dateSave.Substring(4,4));
			int month = Convert.ToInt16(dateSave.Substring(2,2));
			int day = Convert.ToInt16(dateSave.Substring(0,2));
			now = new DateTime(year, month, day);
		}

		saveName = now.ToString ("ddMMyy") + "_" + eventNum;
	}


	/* Set everything to it's default scene creation position and settings, also this method
	 * will set up the questions and display the first one at the bottom of the Event Screen */
	void Start () {

		backgroundImages = GameObject.Find ("EventBackgrounds");
		currentImage = 0;

		backArrow = GameObject.Find ("arrowLeftScreen");
		forwardArrow = GameObject.Find ("arrowRightScreen");

		//The header bar will be hidden when we are question stage 1
		headerBar = GameObject.Find ("HeaderBar");
		headerBar.renderer.enabled = false;

		//Set up the questions and display the first one on the screen by default
		questionStage = 1;
		question1 = "Where were you?";
		question2 = "Who was there?";
		question3 = "Any objects?";
		GameObject questionTag = GameObject.Find ("questionTag");
		questionTag.GetComponentInChildren<TextMesh>().text = question1;

		sceneSaved = false; //The scene hasn't been saved yet

	}

	/* This method will first determine which aspects of the GUI to show i.e. the header bar and
	 * the forward/backward arrows to change backgrounds depending on which stage of event creation
	 * the user is at. It will then check to see if the user has clicked any objects such as placeable
	 * characters or items and then re-direct to the appropiate method such as MovePlaceChar()
	 * 
	 * Since this method runs every frame, we should check if we are currently dragging a character
	 * and if so update its posiiton to the mouse position every frame (this is a very short check
	 * so a new method to house this wasn't created) 
	 * 
	 * Lastly, once all the questions have been answered, the scene will attempt to save to an external
	 * image. This method is called multiple times as it can glitch and not save on the first attempt,
	 * so a variabled called savedScene will check to make sure the scene has been saved and this method
	 * no longer needs to occur */
	void Update () {

		//Determine which arrows to show for switching between backgrounds
		WhichArrows();

		//Check to see if any items have been clicked
		CheckItemSelection();

		//If we are currently clicked on a character/object then update its position to the current mouse position
		if(clickedObj){
			Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector3 movePos = new Vector3(mousePos.x, mousePos.y, -0.4f);
			clickedObj.transform.localPosition = movePos;
		}

		//Attempt to save the scene once we have finished creating it
		if((questionStage > 3) && (!sceneSaved)){
			StartCoroutine(SaveImage());
		}
	
	}

	/* Determine which arrows to show for switching between backgrounds and also whether
	 * or not to show the header bar */
	private void WhichArrows(){

		/* If we are at the first question stage - i.e. user choosing background then don't show the header bar.
		 * Then if we are past this question, no longer show any arrows as background is no longer changeable
		 * without going back a screen and re-entering */
		if(questionStage != 1){

			backArrow.renderer.enabled = false;
			backArrow.collider2D.enabled = false;
			forwardArrow.renderer.enabled = false;
			forwardArrow.collider2D.enabled = false;

			//If we are finished all questions then hide the header bar
			if(questionStage > 3){
				headerBar.renderer.enabled = false;
			}else{
				headerBar.renderer.enabled = true;
			}
		}else{

			//Question 1 - Hide the header bar
			headerBar.renderer.enabled = false;

			//Hide the back arrow on first image and forward arrow on last image
			if(currentImage == 0){
				backArrow.renderer.enabled = false;
				backArrow.collider2D.enabled = false;
			}else if(currentImage == 5){
				forwardArrow.renderer.enabled = false;
				forwardArrow.collider2D.enabled = false;
			}else{
				//Show both arrows to navigate both directions
				backArrow.renderer.enabled = true;
				backArrow.collider2D.enabled = true;
				forwardArrow.renderer.enabled = true;
				forwardArrow.collider2D.enabled = true;
			}
		}

	}

	/* This method will check to see if the left mouse button has been clicked or lifted and act
	 * accordingly. It will check the tag of the clicked object and if a redirect is required then it
	 * is performed. This method also deals with sprite changes such as darkened sprites for items clicked.
	 * This is a very similar method to CheckEventChanges() within the MenuSelect class. */
	private void CheckItemSelection(){

		if(Input.GetMouseButtonDown(0)){
			
			Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			RaycastHit2D hitItem = Physics2D.Raycast(mousePos, Vector2.zero);

			//The user has clicked an item
			if(hitItem.collider != null){
				
				hitObject = hitItem.collider.gameObject;
				int objectPressed = 0;
				
				if(hitObject.tag == "backImageArrow"){
					objectPressed = 1;
					spriteChanged = true;
				}else if(hitObject.tag == "forwardImageArrow"){
					objectPressed = 2;
					spriteChanged = true;
				}else if(hitObject.tag == "questionAnswerTick"){
					objectPressed = 3;
					spriteChanged = true;
				}else if(hitObject.tag == "sceneCharacters"){
					MovePlaceChar(hitObject);
				}else if(hitObject.tag == "sceneObjects"){
					MovePlaceObject(hitObject);
				}else if(hitObject.tag == "deleteObject"){
					//Delete the hit object
					GameObject.Destroy(hitObject.transform.parent.gameObject);
				}
				
				//The old look of the sprite, to revert back if we have clicked version
				if(spriteChanged){
					oldLook = hitObject.GetComponent<SpriteRenderer>().sprite;
				}
				
				switch(objectPressed){
				case (int)itemSelections.BackOption:
					hitObject.GetComponent<SpriteRenderer>().sprite = switchImageArrow;
					StartCoroutine(SlideBackgrounds(false));
					break;
				case (int)itemSelections.ForwardOption:
					hitObject.GetComponent<SpriteRenderer>().sprite = switchImageArrow;
					StartCoroutine(SlideBackgrounds(true));
					break;
				case (int)itemSelections.QuestionAnswer:
					hitObject.GetComponent<SpriteRenderer>().sprite = questionTick;
					nextQuestion();
					break;
				default:
					break;
				}
				
			}
		}

		//When the click has been lifted, revert any changed textures to their original ones
		if(Input.GetMouseButtonUp(0)){
			if(spriteChanged){
				hitObject.GetComponent<SpriteRenderer>().sprite = oldLook;
				spriteChanged = !spriteChanged; //reset back to false;
			}
		}
	}


	/* This method is called when a character has been clicked on, it determines if a character object
	 * has to be created or if we are clicking on an existing one. It also places the character firmly in spot
	 * if we are currently moving the character and the mouse is clicked again */
	private void MovePlaceChar(GameObject clickedOn){

		//If we are out of questions and the scene has saved then do not allow the characters to move anymore
		if(questionStage > 3){
			clickedObj = null;
			objDragging = false;
			return;
		}

		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		int whichChar = 0;

		//If the parent is sceneCharacters then we should create a new character
		if(clickedOn.transform.parent.name == "sceneCharacters"){
			//Which character has been clicked on
			if(clickedOn.name == "adultMale"){
				whichChar = 0;
			}else if(clickedOn.name == "adultFemale"){
				whichChar = 1;
			}else if(clickedOn.name == "olderMale"){
				whichChar = 2;
			}else if(clickedOn.name == "olderFemale"){
				whichChar = 3;
			}else if(clickedOn.name == "teenMale"){
				whichChar = 4;
			}else if(clickedOn.name == "teenFemale"){
				whichChar = 5;
			}else if(clickedOn.name == "babyMale"){
				whichChar = 6;
			}else if(clickedOn.name == "babyFemale"){
				whichChar = 7;
			}else{
				Debug.Log ("The object you have clicked does not exist.");
			}

			clickedObj = Instantiate(charPrefabs[whichChar], mousePos, Quaternion.identity) as GameObject;
			clickedObj.transform.parent = GameObject.Find ("createdScene").transform; //Assign to a specific parent
			objDragging = true;
		}else{
			if(!objDragging){
				//We are clicking on an existing character in the scene
				clickedObj = clickedOn;
				objDragging = true;

				//Hide the delete icon with the character
				foreach(Transform child in clickedObj.transform){
					if(child.name == "deleteObj"){
						child.gameObject.SetActive(false);
					}
				}
			}else{
				//Show the delete icon with the character
				foreach(Transform child in clickedObj.transform){
					if(child.name == "deleteObj"){
						child.gameObject.SetActive(true);
					}
				}

				//Place character down
				clickedObj = null;
				objDragging = false;
			}
		}

	}

	/* This is a similar method to MovePlaceChar but with objects, it is called when a character has been clicked on, 
	 * it determines if an object has to be created or if we are clicking on an existing one. It also places the 
	 * object in spot if we are currently moving it and the mouse is clicked again */
	private void MovePlaceObject(GameObject clickedOn){

		//If we are out of questions and the scene has saved then do not allow the objects to move anymore
		if(questionStage > 3){
			clickedObj = null;
			objDragging = false;
			return;
		}
		
		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		int whichObj = 0;
		
		//If the parent is sceneObjects then instantiate a new object
		if(clickedOn.transform.parent.name == "sceneObjects"){
			//Which object has been clicked
			if(clickedOn.name == "objBalloons"){
				whichObj = 0;
			}else if(clickedOn.name == "objCake"){
				whichObj = 1;
			}else if(clickedOn.name == "objFootball"){
				whichObj = 2;
			}else if(clickedOn.name == "objTennisBall"){
				whichObj = 3;
			}else if(clickedOn.name == "objMail"){
				whichObj = 4;
			}else{
				Debug.Log ("The object you have clicked does not exist.");
			}
			
			clickedObj = Instantiate(objPrefabs[whichObj], mousePos, Quaternion.identity) as GameObject;
			clickedObj.transform.parent = GameObject.Find ("createdScene").transform; //Assign to specific parent
			objDragging = true;
		}else{
			if(!objDragging){
				clickedObj = clickedOn;
				objDragging = true;

				//Hide the delete icon with the object
				foreach(Transform child in clickedObj.transform){
					if(child.name == "deleteObj"){
						child.gameObject.SetActive(false);
					}
				}
			}else{
				//Show the delete icon with the object
				foreach(Transform child in clickedObj.transform){
					if(child.name == "deleteObj"){
						child.gameObject.SetActive(true);
					}
				}

				//Place object down
				clickedObj = null;
				objDragging = false;
			}
		}
		
	}

	/* Increment the current question to ask the user and change the screen accordingly */
	private void nextQuestion(){

		questionStage++;
		GameObject questionTag = GameObject.Find ("questionTag");

		if(questionStage == 2){
			questionTag.GetComponentInChildren<TextMesh>().text = question2;
			StartCoroutine(SlideCharacters(true)); //Slide the characters into the bar at the top
		}else if(questionStage == 3){
			questionTag.GetComponentInChildren<TextMesh>().text = question3;
			StartCoroutine(SlideCharacters(false)); //Slide the characters out of view
			StartCoroutine(SlideObjects(true)); //Slide the objects into view
		}else{
			questionTag.GetComponentInChildren<TextMesh>().text = "Event Saved";

			StartCoroutine(SlideObjects(false)); //Slide the objects out of view
			hideDeleteIcons();

			//Hide the tick mark
			GameObject questionAnswer = GameObject.Find ("questionAnswer");
			questionAnswer.renderer.enabled = false;
		}

	}
	
	/* A co-routine for saving a snapshot of the completed scene in and storing in the
	 * Resources folder. We use this image for showing the user the scene later on when
	 * they are on the EventViewScreen or the TodayAddEventScreen */
	IEnumerator SaveImage(){

		yield return new WaitForEndOfFrame();

		//Make sure the objects are out of the frame before taking the screenshot
		GameObject sceneObjects = GameObject.Find ("sceneObjects");
		Vector3 objPos = sceneObjects.transform.position;
		Vector3 endPos = new Vector3(-2.2f, 6.0f, -0.4f);

		//Hide the back arrow and question tag
		GameObject backArrow = GameObject.Find ("backArrowEvent");
		GameObject questionTag = GameObject.Find ("questionTag");
		backArrow.renderer.enabled = false;
		questionTag.renderer.enabled = false;

		if((questionStage > 3) && (!sceneSaved) && (objPos == endPos)){
			
				//Capture the current screen to a texture
				Texture2D renderShot = new Texture2D(1024, 768);
				renderShot.ReadPixels(new Rect(0, 0, 1024f, 768f), 0, 0);
				renderShot.Apply();

				//Encode the texture to a png
				byte[] renderPNG = renderShot.EncodeToPNG();
				Destroy(renderShot);

				//Save the image file in the Screenshots folder
				File.WriteAllBytes(Application.dataPath + "/Resources/" + saveName + ".png", renderPNG);

				sceneSaved = !sceneSaved;

				//Pass a message to the MenuSetup class that a new scene has been saved
				MenuSetup menuSet = this.GetComponent<MenuSetup>();
				menuSet.setNewSavedImage(saveName);

				//Show the back arrow and question tag again
				backArrow.renderer.enabled = true;
				questionTag.renderer.enabled = true;
		}

		yield return null;

	}
	

	/* A method for when all the questions have finished and the scene can no longer be edited then hide
	 * the delete icons that each character/object has at the top right */
	private void hideDeleteIcons(){

		//Loop through all characters/objects in the created scene
		GameObject createdScene = GameObject.Find ("createdScene");
		int numChildren = createdScene.transform.childCount;
		for(int i = numChildren - 1; i >= 0; i--){
			//Now that we are on each char/obj, hide their delete icon
			GameObject crtObj = createdScene.transform.GetChild(i).gameObject;
			foreach(Transform child in crtObj.transform){
				if(child.name == "deleteObj"){
					child.gameObject.SetActive(false);
				}
			}
		}

	}

	/* A co-routine to run a lerp to switch between different backgrounds, the boolean that is passed
	 * in determines if the new image should swipe from the left or from the right */
	IEnumerator SlideBackgrounds(bool isGoingForward){
		
		//Make sure we are not trying to move the same screen on top of itself
		//and that we are currently not already in a screen slide
		if(!screenSlideProgress){

			screenSlideProgress = true;

			Vector3 startPos = backgroundImages.transform.position;
			Vector3 endPos = new Vector3();

			//Determine whether to place the new image left or right of the current image
			if(isGoingForward){
				endPos = startPos + new Vector3(-10.25f, 0, 0);
				currentImage++;
			}else{
				endPos = startPos + new Vector3(10.25f, 0, 0);
				currentImage--;
			}

			float moveTime = 0.0f;

			while(moveTime < 1.1f){
				backgroundImages.transform.position = Vector3.Lerp (startPos, endPos, moveTime);
				moveTime += Time.deltaTime * 2.5f;
				yield return null;
			}
			
			//Screen slide progress is finished so set the progress to false
			screenSlideProgress = !screenSlideProgress;
		}
		
	}

	/* A co-routine to run a lerp to slide the characters in, this is called when the user switches
	 * to the second question and then when they change to the third question, they slide out of view */
	IEnumerator SlideCharacters(bool comingIn){
			
		GameObject sceneCharacters = GameObject.Find ("sceneCharacters");
		Vector3 startPos = sceneCharacters.transform.position;
		Vector3 endPos = new Vector3(-3.0f, 3.15f, -0.4f);

		//If we are moving the characters out of view
		if(!comingIn){
			endPos.y = 5.0f;
		}
			
		float moveTime = 0.0f;
			
		while(moveTime < 1.1f){
			sceneCharacters.transform.position = Vector3.Lerp (startPos, endPos, moveTime);
			moveTime += Time.deltaTime * 2.5f;
			yield return null;
		}

	}

	/* A co-routine to run a lerp to slide the scene objects in, this is called when the user switches
	 * to the third question and then when they save the event, they slide out of view */
	IEnumerator SlideObjects(bool comingIn){
		
		GameObject sceneObjects = GameObject.Find ("sceneObjects");
		Vector3 startPos = sceneObjects.transform.position;
		Vector3 endPos = new Vector3(-2.2f, 3.15f, -0.4f);
		
		//If we are moving the objects out of view
		if(!comingIn){
			endPos.y = 6.0f;
		}
		
		float moveTime = 0.0f;
		
		while(moveTime < 1.1f){
			sceneObjects.transform.position = Vector3.Lerp (startPos, endPos, moveTime);
			moveTime += Time.deltaTime * 2.5f;
			yield return null;
		}
		
	}
}
