# edit-plantuml

plantuml editor with preview that supports drag-and-drop to Azure Devops Wiki

## Installation

````
C:\>dotnet tool install -g edit-plantuml
You can invoke the tool using the following command: edit-plantuml
Tool 'edit-plantuml' (version '0.1.0') was successfully installed.
```` 

## Usage

````
C:\>edit-plantuml
````

Drag a plantuml image (png) from any website into the preview pane. The plantuml source code will be recovered from the image and you can edit.

When done with editing, you can drop the plantuml image from the preview pane to your local file system or to a drop-upload-enabled web site (e.g. Azure Devops Wiki)

# Amg.Plantuml

fast C# wrapper lib for plantuml.

* Speeds conversion up by keeping the plantuml process running between uses.
* Automatically downloads plantuml if not found in the default install location.

## Usage

Nuget:
~~~~
donet add package Amg.Plantuml
~~~~

Code:

~~~~C#
	var plantuml = Amg.Plantuml.Plantuml.Local();
	await plantuml.Convert("A --> B", "out.png");
~~~~

