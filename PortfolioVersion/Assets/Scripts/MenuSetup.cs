using UnityEngine;
using System;
using System.Collections;
using System.IO;

/* Visual Diary (Originally May 2013 - Updated October/Novemeber 2014
 * Original concept and prototype: 
 * Chris Forgie, Gavin Whitehall, Ryan Welsh, Chris Marshall and Karla Zero
 * GameJam (Jamming for Small Change May 2013 - Glasgow Caledonian University)
 * 
 * On October/Novemember 2014, the project was completely re-written from scratch
 * The original prototype was missing several features and used a third party 
 * plugin for Unity which can't be distrubted without the proper licensing.
 * The code was re-written for the Unity2D part of Unity3D, missing features
 * were added, existing bugs were fixed and the code was completely overhauled
 * and optimised by Chris Forgie. The original images and sprites are the same 
 * from the prototype drawn by Chris Marshall and Karla Zero */


/* This Class is primarily used for setting up various screens within this application.
 * On application start, the Main Menu screen and the Calendar screen are only set up 
 * once and won't change throughout the run time of this program.
 * 
 * This class also sets up the add event screen and displays any recorded events on that
 * day recordingly. The call for the SetUp is actually called from the SwitchScreens method
 * in the MenuSelect class since we can't predict which day to load at start up - (user
 * could select any previous day from the calendar) */

public class MenuSetup : MonoBehaviour {

	//Variables required for the Calendar Screen
	enum daysOfWeek {Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday};
	public GameObject calendarDate; //Date object to be instantiated for each day of month

	//Add event screen variables
	public GameObject[] eventButtons;
	private bool newImageSaved = false; //Check for new images saved
	private string newsavedName;


	/* Called at runtime once, sets up the main menu and calendar screens using the current date */
	void Start () {

		DateTime now = DateTime.Now;

		setUpMainMenuScreen(now);
		setUpCalendarScreen(now);
	}


	/* Update is called once per frame
	 * This update function is used for when a new image has been saved (an event created)
	 * It will load the newly created image into a texture and render it to the Add Event screen
	 * 
	 * The reason this is called in the update method (which runs every frame) is because the image
	 * doesn't seem to load and render on one call, the function will carry out and the texture will change
	 * but it will just appear blank. So a check is performed, until the Add Event screen correctly shows
	 * the newly created event, this method will be called once per frame */
	void Update () {

		if(newImageSaved){
				
			//Load Texture
			Texture2D imgLoad = (Texture2D)Resources.Load (newsavedName) as Texture2D;
					
			//Enable the renderer on the add event screen to show this saved image
			GameObject shownImage = eventButtons[Convert.ToInt16(newsavedName.Substring(7,1)) - 1].transform.GetChild(0).gameObject;
			shownImage.renderer.enabled = true;
			Texture oldTex = shownImage.renderer.material.mainTexture;
			shownImage.renderer.material.mainTexture = imgLoad;

			if(oldTex != imgLoad){
				newImageSaved = false;
			}
		}

	}


	/* This method is called from the Start function of this class, it is only called once per run time
	 * It is used to set up the dynamic variables of the main menu screen which are the current month and date */
	void setUpMainMenuScreen(DateTime now){

		GameObject todayMonth = GameObject.Find ("todayMonthText");
		GameObject todayDate = GameObject.Find ("todayDateText");
		todayMonth.GetComponentInChildren<TextMesh>().text = now.ToString("MMMM");
		todayDate.GetComponentInChildren<TextMesh>().text = now.ToString("dd");
	}


	/* This method is called from the Start function of the class and is only called once per runtime
	 * It generates a calendar for the current month. The calendar background is a sprite with empty boxes
	 * left to fill the spaces with dates.
	 * 
	 * This method will instantiate the correct number of date objects, name/tag and position them correctly.
	 * 
	 * Currently due to the sprite of the calendar, if the month has 30 or more days and the first day is a 
	 * Saturday then the 30th and 31st date objects will position incorrectly on the calendar image although 
	 * they still work as expected and is only an aesthic issue. */
	void setUpCalendarScreen(DateTime now){

		GameObject calendarMonth = GameObject.Find ("calendarMonth");
		calendarMonth.GetComponentInChildren<TextMesh>().text = now.ToString("MMMM");
		
		int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
		int weekRow = 0; //Row on the calendar to display the date (Sun - Sat)
		
		//For each day in the monthm instantiate a calendarDate object and position this accordingly
		for(int i = 0; i < daysInMonth; i++){
			
			//Offsets required to position each date (text) correctly into a calendar box
			float xOffset = 1.12f;
			float yOffset = -0.74f;
			
			int dayNumber = i+1;
			
			/* Get the current day as a number between 0-6 (Sun-Sat) and form a switch statement
			 * to correctly position the date  */
			DateTime currentDay = new DateTime(now.Year,now.Month,i+1);
			int weekDayNum = (int)(daysOfWeek)Enum.Parse(typeof(daysOfWeek), currentDay.ToString("dddd"));
			
			//Instantiate the newDate object and set this as a child of calendarDateStart (for positioning)
			GameObject newDate = Instantiate(calendarDate) as GameObject;
			string prefixDayNum = (dayNumber < 10) ? ("0" + dayNumber) : ("" + dayNumber); //Prefix a 0 if required
			newDate.name = prefixDayNum + now.ToString ("MMyyyy");
			newDate.transform.parent = GameObject.Find ("calendarDateStart").transform;
			newDate.transform.localPosition = Vector3.zero;
			newDate.tag = "calendarDate";
			
			newDate.GetComponentInChildren<TextMesh>().text = dayNumber.ToString(); //Display the right date
			
			//Use a vector3 to correctly position the newly created date into a calendar slot
			Vector3 newPos = new Vector3(newDate.transform.localPosition.x, 0, 0);
			
			switch(weekDayNum){
			case (int)daysOfWeek.Sunday:
				//If today is not the first then move the row down to display properly (not overlap)
				if(dayNumber != 1){ weekRow += 1; }
				break;
			case (int)daysOfWeek.Monday:
				newPos.x = xOffset;
				break;
			case (int)daysOfWeek.Tuesday:
				newPos.x = xOffset * 2;
				break;
			case (int)daysOfWeek.Wednesday:
				newPos.x = xOffset * 3;
				break;
			case (int)daysOfWeek.Thursday:
				newPos.x = xOffset * 4;
				break;
			case (int)daysOfWeek.Friday:
				newPos.x = xOffset * 5;
				break;
			case (int)daysOfWeek.Saturday:
				newPos.x = xOffset * 6;
				break;
			default:
				break;
			}
			
			//Double digits numbers centre slightly outside (left) of a calendar box so align these slightly to the right
			if(dayNumber > 9){
				newPos.x += 0.07f;
			}
			
			/* Now that we have the correct x position (horizontal row on calendar)
			 * Position the vertical row according to the weekRow defined by each Sunday */
			newPos.y = weekRow * yOffset;
			
			//Lastly update the instantiated dates final position
			newDate.transform.localPosition = newPos;
		}
	}


	/* This method is called from the updateEventDay method in this class which is respectively called from the
	 * SwitchScreens function in the MenuSelect class.
	 * When the user switches to the Add Event screen. This method is not called initially 
	 * as this screen differs for different days. A user could use the today button or 
	 * retrieve a different day from the calendar
	 * 
	 * This screen also has to be re-generated every time a new event has been created
	 * or the user has chosen a different date so that it can display the correct events accordingly */
	void setUpAddEventScreen(string displayDay){

		DateTime dateChosen;

		//A displayDay of 0 represents the current day, otherwise a string of ddMMYYYY will be parsed
		if(displayDay == "0"){
			dateChosen = DateTime.Now;
		}else{
			int year = Convert.ToInt16(displayDay.Substring(4,4));
			int month = Convert.ToInt16(displayDay.Substring(2,2));
			int day = Convert.ToInt16(displayDay.Substring(0,2));
			dateChosen = new DateTime(year, month, day);
		}
		
		string partialName = dateChosen.ToString ("ddMMyy");
	
		//Search for each image (saved event) created on the supplied date
		DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/Resources/");
		FileInfo[] dirFiles = dir.GetFiles("*" + partialName + "*.png*");

		//Loop through each event found (Max. 6) and display them accordingly
		foreach (FileInfo file in dirFiles)
		{
			string fileName = (file.Name).Substring(0,8);
			string eventNum = fileName.Substring(fileName.LastIndexOf('_') + 1, 1);

			Texture2D imgLoad = (Texture2D)Resources.Load (fileName) as Texture2D;
			
			//Enable the renderer on the add event screen to show this saved image
			GameObject shownImage = eventButtons[Convert.ToInt16(eventNum) - 1].transform.GetChild(0).gameObject;
			shownImage.GetComponent<BoxCollider2D>().enabled = true;
			shownImage.renderer.enabled = true;
			shownImage.renderer.material.mainTexture = imgLoad;
		}

		//Display the date supplied at the top of this screen, also a check to display the correct suffix
		GameObject addEventTitle = GameObject.Find ("addEventTitle");
		string eventTitle = dateChosen.ToString("dddd d");

		switch(dateChosen.Day % 10){
			case 1:
				eventTitle += "st"; break;
			case 2:
				eventTitle += "nd"; break;
			case 3:
				eventTitle += "rd"; break;
			default:
				eventTitle += "th"; break;
		}

		eventTitle += " " + dateChosen.ToString("MMMM");
		addEventTitle.GetComponentInChildren<TextMesh>().text = eventTitle;
	}


	/* This function is called from the EventCreate class, when a new image has been saved, we set the
	 * newImageSaved value to true, then inside the update loop of this class, if this value is true, 
	 * we load the new image to the add event screen */
	public void setNewSavedImage(string savedName){

		newImageSaved = true;
		newsavedName = savedName;
	}


	/* This method is called from the SwitchScreens method in the MenuSelect class when the user wants to
	 * go the add event screen, the date supplied could be today or a day selected from the calendar
	 * 
	 * First clear all the current pictures (events) on this screen if some have been previously loaded and
	 * set the screen back to its default settings. Then call the setUpAddEventScreen method in this class
	 * using the date supplied */
	public void updateEventDay(string dayUpdate){

		foreach(GameObject evButton in eventButtons){
			evButton.transform.GetChild (0).gameObject.GetComponent<BoxCollider2D>().enabled = false;
			evButton.transform.GetChild (0).gameObject.renderer.enabled = false;
		}

		setUpAddEventScreen(dayUpdate);
	}
	
}
