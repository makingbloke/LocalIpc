# LocalIpc

Copyright ©2021 Mike King.  
Licensed using the MIT licence. See the License.txt file in the solution root for more information.  

## Overview

LocalIpc is a simple library for communicating between two objects. The objects can be contained in the same or different processes.

## Pre-Requisites

This solution uses .Net 6.0. 

## Usage

Local Ipc uses two classes LocalIpcClient and LocalIpcServer. 

The LocalIpcServer object is created using the LocalIpcServer.Create factory method. The client is then created / launched and finally the server class is initialized by calling InitializeAsync.

The LocalIpcClient object is created using the LocalIpcClient.Create factory method. It is passed the send and receive handles from the server. Then the class must be initialized by calling the Initialize method.

See the tests (LocalIpcTests.cs) for examples on how to create a simple client and server. Objects can be received either using the Receive method or by setting the IsReceiveEventsEnabled property to true and attaching to the Received event.

## Serialization

See the ISerializer interface for details of the serialization methods used by LocalIpc. The default serializer uses JSON.Net. You can replace the default serializer by writing your own and passing it as a parameter into the Create methods.

Any serializer must be able to add the type of the object being serialized to the serialized data being passed to the client.
