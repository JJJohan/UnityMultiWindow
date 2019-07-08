#pragma once

#include <GL/glew.h>
#include <SDL.h>
#include <string>

class Window
{
public:
	Window(std::string title, HGLRC unityContext, int width, int height, bool resizable, GLuint textureHandle);
	~Window();

	bool CreateContext();
	void Render();
	void HandleEvent(const SDL_Event& event);
	void SetPosition(int x, int y) const;
	void Drag() const;

	static CloseFunction CloseDelegate;
	static ResizeFunction ResizeDelegate;
	static MouseUpdateFuncton MouseDelegate;
	static MoveFunction MoveDelegate;
	static void LoadResources();
	static void UnloadResources();

	unsigned int ID;

private:
	SDL_Window* _pWindow;
	HGLRC _unityContext;
	HDC _deviceContext;
	GLuint _pTextureHandle;
	std::string _title;
	int _width;
	int _height;
	bool _resizable;
	bool _focused;

	static GLuint _vao;
	static GLuint _vbo;
	static GLuint _ebo;
	static GLuint _fragmentShader;
	static GLuint _vertexShader;
	static GLuint _shaderProgram;
};