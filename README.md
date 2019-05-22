Forge Build Tool
================

The Forge Build Tool is a task based custom tool to manage the process of building source code across a variety of projects,
coding languages and configurations. It supports different kinds of project accumulation with or without explicite defined and
maintained build files.

In the default configuration, the project is split into modules, where each folder contained in the project path counts as a 
module/ a project on its own. Each module has covers all files that are placed below the module root folder and can control how
it is built using **Mixins**. Any other options for defining module dependencies, additional libraries, assembly type, etc. are
handled by static code analysis as soon as the module/ project is build.

Configuration
-------------

Forge is designed arround certain default behavior, anyways it can be extended using the mixin API.

The mixin API handles a well chosen set of entry configuration entry points open up the capability to extend and even
override the default behavior using C# classes defined in .Build.cs files. Those files are collected once at startup from
the SDK directory but can also be defined on a per-project base. You need to append behavior and make use of conditional
code that applies to projects created from that directory only.

Build Scripts
-------------

In addition to the default and mixin user defined behavior, Forge also provides an API to execute scripts. A path pushed to
the program parameters at startup is interpreted as script and processed by the runtime.

The runtime provides a generic single line command based shell scripting environment capable to locate and execute commands
either on the local platform or defined as Method in C# source code.

The configuration created from the script is populated to Forge by the Store, a central place to keep values and its coresponding
commands to add and modify those values. Some well named values are however detected by the building process and applied as
additional configuration.

A special challenge is the parallel processing environment leading to some restrictions and exceptions to a generic shell.
Commands can't be able to redirect the standard input and output channels and most piping operators should not be used.

Task Graph
----------

The build processing in Forge is split into a set of conditionally linked nodes forming the task graph, heavily depending on
the configuration passed and the projects/ modules defined, while each node is a single Task to be executed.

Task execution is handled by the build profile that is (depending on the kind of profile) executed on the local maschine, on
a remote maschine or cloud service in parallel. The graph always seeks for the highest possible parallelization to reduce
building time even on huge projects.

The graph is automatically generated and dosen't need any previous configuration. Each node defines a number of input and
conditional number of output pins that may be different in amount and consistency. Output pins are connected to the best
matching input pins of a well defined kind and type of data stored on them after execution is finished, as long as there
is still data be placed on them. This leads to a growing graph of actions performed while processing the build request.

A twig of the graph is canceled on error so an incomplete build can't happen.

Modules
-------

Modules are the building blocks of source code to cover certain functionality and Forge can handle different kinds of modules
to build up your project. Huge projects like a Game Engine are implemented as a large collection of modules that may depend
on each other, and games often supply their own modules to augment them.

Forge handles every folder under certain project's root directory as an own module (a sub-module) of the project.

Forge also provides a mixin to define certain directory as module directory. The same rules apply as for generic sub-modules
except that those modules will be available system-wide. Such global modules can be linked statically even in those languages
that don't support static linking by default.

Every dependency to sub-modules or global modules are automatically resolved from static code analysis.

Project Files for IDEs
----------------------

If you download the latest source code, you might notice that there are no project files included for compiling and running the
code with your target IDE like Visual Studio or Visual Studio Code. This is because such project files can contain configurations
that might not be compatible with your platform or version of the IDE you like to work with. There might also be situations where
you want to test code in different versions of the same IDE.

Forge supports different built-in task nodes that accept an input project to generate such IDE relevant files for you and takes
care of the IDEs and versions you have installed on your platform.

Build Targets
-------------

As for project files, Forge tasks nodes can open a wiede varity of possible build targets to choose. Targets are declared
through C# source files added directly to the program, via mixin or plugin. Each target declares a class deriving from the
ITaskPrototype interface, and sets properties controlling how it should be located to the factory. When asked to build a 
specific target, Forge will construct a Task based on the target's class and attach it to the build graph if possible.

Branches
--------

The **[master branch](https://github.com/SchroedingerEntertainment/Forge/tree/master)** is the current main development branch and may however be buggy. We try hard to keep this branch in a compilable state and aim to publish new releases from time to time.

Other short-lived branches may pop-up from time to time as we stabilize new releases or hotfixes.

Contributions
-------------

We welcome any contributions to this content's development through [pull requests](https://github.com/SchroedingerEntertainment/Forge/pulls/) on GitHub. Most of our active development is in the **master** branch, so we prefer to take pull requests there. We try to make sure that all new code adheres to the [Schroedinger Entertainment General Coding Standard](https://github.com/SchroedingerEntertainment/Guidelines/blob/master/Coding%20Standard.md).

All contributions are governed by the terms of the EULA and however, guided by the [Contribution Code of Conduct](https://github.com/SchroedingerEntertainment/Guidelines/blob/master/Contribution%20Code%20of%20Conduct.md).

Licensing
---------

Copyright (C) 2017 Schroedinger Entertainment

Your access to and use of this content on GitHub is governed by the [Schroedinger Entertainment End User License Agreement](https://github.com/SchroedingerEntertainment/License/blob/master/EULA.md). If you don't agree to those terms, you are not permitted to access or use this content.

(The Agreement includes the [GNU Affero General Public License](https://github.com/SchroedingerEntertainment/Forge/blob/master/LICENSE))
