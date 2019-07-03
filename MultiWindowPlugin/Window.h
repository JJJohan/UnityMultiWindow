#pragma once

#include <GL/glew.h>
#include <GL/wglew.h>
#include <SDL.h>
#include <string>

class Window
{
public:
	Window(std::string title, HGLRC unityContext, HDC unityDevice, int width, int height, bool resizable, GLuint textureHandle);
	~Window();

	bool CreateContext();
	bool Render();

	static CloseFunction CloseDelegate;
	static ResizeFunction ResizeDelegate;
	static void LoadResources();
	static void UnloadResources();

private:
	SDL_Window* _pWindow;
	HDC _unityDevice;
	HGLRC _unityContext;
	HDC _deviceContext;
	GLuint _pTextureHandle;
	std::string _title;
	int _width;
	int _height;
	bool _resizable;
	bool _quit;

	static GLuint _vao;
	static GLuint _vbo;
	static GLuint _ebo;
	static GLuint _fragmentShader;
	static GLuint _vertexShader;
	static GLuint _shaderProgram;
};