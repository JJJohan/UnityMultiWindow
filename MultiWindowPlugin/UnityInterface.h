#pragma once

#include <string>

#define DllExport __declspec(dllexport)

class Window;

typedef void (__stdcall *MessageFunction)(const char* message);
typedef void (__stdcall *CloseFunction)(Window* window);
typedef unsigned int (__stdcall *ResizeFunction)(Window* window, int width, int height);

extern "C"
{
	DllExport void InitPlugin(MessageFunction messageDelegate, CloseFunction closeDelegate, ResizeFunction resizeDelegate);
	DllExport void ShutdownPlugin();

	DllExport Window* CreateNewWindow(const char* title, int width, int height, bool resizeable, unsigned int textureHandle);
	DllExport void UpdateWindows();
	DllExport void DisposeWindow(Window* windowHandle);
}

void Log(std::string message);