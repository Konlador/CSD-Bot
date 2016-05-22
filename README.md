# Cook Serve Delicious Bot
As the title says, a program that automatically plays "Cook, Serve, Delicious!"

I programmed this with C# for a school project called "Personal project" (pp for short)

You may freely to use this source-code

### Process
- Just after starting the program it tries to look for a text file called "Menu.txt". The text file contains a list of recipes for each possible dish in the game. Opens the text file, reads the recipes and saves in a dictionary.
- If the game is running and the user has pressed the "on" button or F12 program starts to constantly check if there is an order in queue. It checks by looking at the coordinates of all slot numbers from 1-8 and if the color of the number is white the slot number is put in the queue of preparing the dish.
- If there is something to prepare the program presses the slot number, reads the title of the dish with a text recognition algorithm and starts to prepare. (If other dishes are finished cooking while preparing something they are delivered in the process)

### Requirements
- Only works on windows operating system (text recognition algorithm)
- Need to modify (hard code) the coordinates if not using a 1680x1050 resolution monitor

### Need to know
As this is still a work in progress project it may hiccup sometimes and fail to prepare a dish (not likely).

Can't do complex dishes such as baked potatoes or coffee when it is required to look at the description how much sugar must be added.
