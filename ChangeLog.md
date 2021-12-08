### Version 1.0.0.0 - 1st November 2021 

* Initial release.

### Version 1.0.1.0 - 1st November 2021 

* Upgraded to .Net 6 and fixed timing issue which caused the ```TestSendExternalClientBrokenPipeAsync``` test to fail under some circumstances.

### Version 1.0.1.1 - 3rd December 2021 

* Tidied up the broken pipe exception trapping code.
* Updated Test Adapter and Test Framework Nuget packages.
* Simplified the ```ReceivedEventArgs``` class. Replaced the ```GetValue``` method with a property ```Value``` and removed the ```GetValue<T>```. Use ```as T``` or similar to convert ```Value``` to the required type.

### Version 1.0.2.0 - 8th December 2021 

* Removed dependency on Newtonsoft Json.Net in DefaultSerializer and replaced it with System.Text.Json.
