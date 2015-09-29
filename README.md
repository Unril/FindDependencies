# FindDependencies
This tool uses [Dependency Walker](http://www.dependencywalker.com/) to create batch script that copies all executable's dependencies found by given paths to separate folder. You can then copy them to the same directory with executable. It is an alternative of adding those path to the global Path environment variable. Note that is searches *unmanaged* dlls for *unmanaged* executables written, for example, on C++. 

For example we have test.exe that depends on Qt, OpenMesh and Coin3D. We want to find all dlls it loads during startup and place them near test.exe. Download [FindDependencies](https://github.com/Unril/FindDependencies/releases/tag/v1.0), also download [Dependency Walker](http://www.dependencywalker.com/) and place near it. Open cmd and type: 
```bat
FindDependencies.exe c:\bin\test.exe c:\testsPaths.txt
```

Where test.exe — executable to analize and testPaths.txt — text file contains paths where to search for dependencies. It can contain absolute paths and environment variables. For example:
```bat
%QTDIR%\5.3\msvc2013_64_opengl\bin
c:\openmesh\bin
%CPPLIBS%\Coin3D\bin
```

At the end there will be a folder named 'test' in the current directory with copydlls.bat file in it. Run it and it will copy all dlls to the 'bin' directory near it. Your can also can copy bat file content to afterbuild action (with some modifications).

It is build with Visual Studio 2015 and requires .net framework v4.5.2.
