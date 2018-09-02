# SpeedCam

This program processes footage from a fixed position camera and logs time/speed for vehicles that are travelling horizontally across the screen.  It's tailored to my particular footage but could be easily modified for other scenarios.

## Requirements
The following are needed to run the program without modification.  

* Visual Studio 2017
* DotNet Core Runtime
* FFMpeg
* SQL Server

## Setup

* Add a file named "setup.cfg" to the same directory as the DLLs.  This should only contain the connection string to the database
* Run the setup.sql file against your database to create the necessary tables.
* Add a row to the Config table with your values.  More information on each settings is listed below.

## How Does It Work?

The main process is split up into 3 parts:  export the raw video from the device, convert it to mp4, and analyze the video to identify cars and speed.

### Export

I'm using an Amcrest NVR which allows exporting footage in the DAV format via a HTTP API.  The assumption in this case is that the camera is always recording so footage is retrieved by date.  If your device only records on motion you can query a list of files and export them that way.

### Conversion

The raw file is in the DAV format which seems to be typically used by Chinese camera manufacturers but is not usable for most media players or OpenCV.  We use FFMpeg to do a forced conversion to x264 in a MP4 container.  It's a messy conversion and you will lose about 10 seconds worth of frames in a 10 minute file.  

### Identification

The interesting part of the program.  OpenCV is used to subtract the background per frame and group the blobs of movement into a rectangle that surrounds the vehicle.  Each rectangle is tracked as it moves across the screen.  The number of frames is counted when the leading edge of the rectangle intersects your known start/stop points.  You'll need to know the distance between these two points so that the speed can be calculated.  

## Configuration

* **StartTimeSunrise** - the minutes after sunrise to start exporting video.  A negative value would be before sunrise
* **EndTimeSunset** - the minutes after sunset to stop exporting video.
* **LeftDistance** - the distance in feet that vehicles travelling to the left will traverse
* **RightDistance** - the distance in feet that vehicles travelling to the right will traverse
* **LeftStart** - the left edge (number of pixels)
* **RightStart** - the right edge (Position 0,0 is at the top left for videos)
* **Latitude** - the latitude of the camera
* **Longitude** - the longitude of the camera (Lat/Long used to calculate sunrise/sunset times)
* **ExportFolder** - where to export DAV files
* **ConvertedFolder** - where to save the MP4 files after converting from DAV
* **ConvertedErrorFolder** - occasionally the conversion to MP4 from DAV fails, move the DAV file here
* **AnalyzedFolder** - where to move MP4 files to after they are done analyzing
* **PhotoFolder** - photos with timestamps/speeds of individual vehicles is saved here
* **VideoAddress** - the path to the camera/NVR ex. http://127.0.0.1
* **VideoUser** - the username to use to access the API
* **VideoPassword** - the password for the API
* **VideoChannel** - my NVR has multiple devices, this integer specifies which one to access
* **ChunkTime** - When exporting footage, pull this many minutes at a time
