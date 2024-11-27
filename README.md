I needed to send the headphones battery status to home assistant.

![image](https://github.com/user-attachments/assets/9be86a8e-70a1-4263-b5c8-a08ba8e959d0)

- Required binary: https://github.com/libusb/hidapi/releases/tag/hidapi-0.14.0
- Required environment variable: "HAS_API_KEY" : "home assistant api key"
- HA address hard coded "http://192.168.88.40:30027/";

Can run as a service by doing
- [Run as admin] sc.exe create ZeeHomeAssistantToolsetService binPath= "path\to\binary\Toolset.exe"
And then making it auto start from services
