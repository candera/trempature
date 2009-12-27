Trempature - an outdoor temperature monitor that runs in the tray

Trempature is a simple program that puts an icon in the system notification
area (also known as the tray) displaying the outdoor temperature. It updates
once a minute. If it can't get the temperature for more than half an hour, 
the background turns red. 

Limitations: 

* Currently hardcoded to display the temperature at MSP airport. 
* Always displays the temperature in degrees farenheight. 
* Written by me in about half an hour. :) 

If you want to display a different location, either wait for me to 
update the program (no promises on schedule) or change the URL in 
Form1.cs (RetrieveTemperature) to one of the XML URLs listed in
http://www.weather.gov/xml/current_obs/index.xml. 

Questions, comments, requests: contact me at candera@wangdera.com.

-Craig Andera
December 27th, 2009