# Amg.Plantuml

Fast C# wrapper for plantuml. 

Speeds conversion up by keeping the plantuml process running between uses.

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
