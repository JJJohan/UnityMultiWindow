#include "UnityInterface.h"
#include "Window.h"
#include "SDL_syswm.h"

CloseFunction Window::CloseDelegate = nullptr;
ResizeFunction Window::ResizeDelegate = nullptr;

GLuint Window::_vao = 0;
GLuint Window::_vbo = 0;
GLuint Window::_ebo = 0;
GLuint Window::_fragmentShader = 0;
GLuint Window::_vertexShader = 0;
GLuint Window::_shaderProgram = 0;

Window::Window(std::string title, HGLRC unityContext, HDC unityDevice, int width, int height, bool resizable, GLuint textureHandle)
	: _pWindow(nullptr)
	, _title(title)
	, _pTextureHandle(textureHandle)
	, _deviceContext(0)
	, _unityContext(unityContext)
	, _unityDevice(unityDevice)
	, _width(width)
	, _height(height)
	, _resizable(resizable)
{
}

bool Window::CreateContext()
{
	unsigned int windowFlags = SDL_WINDOW_OPENGL | SDL_WINDOW_SHOWN;
	if (_resizable)
	{
		windowFlags |= SDL_WINDOW_RESIZABLE;
	}

	_pWindow = SDL_CreateWindow(_title.c_str(), SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, _width, _height, windowFlags);
	if (_pWindow == nullptr)
	{
		return false;
	}

	SDL_SysWMinfo info;
	SDL_VERSION(&info.version);
	SDL_GetWindowWMInfo(_pWindow, &info);
	_deviceContext = info.info.win.hdc;

	return true;
}

void Window::LoadResources()
{
	// Create Vertex Array Object
	glGenVertexArrays(1, &_vao);
	glBindVertexArray(_vao);

	// Create a Vertex Buffer Object and copy the vertex data to it
	glGenBuffers(1, &_vbo);

	GLfloat vertices[] = {
		//  Position      Texcoords
			-1.0f,  1.0f, 0.0f, 1.0f, // Top-left
			 1.0f,  1.0f, 1.0f, 1.0f, // Top-right
			 1.0f, -1.0f, 1.0f, 0.0f, // Bottom-right
			-1.0f, -1.0f, 0.0f, 0.0f  // Bottom-left
	};

	glBindBuffer(GL_ARRAY_BUFFER, _vbo);
	glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

	// Create an element array
	glGenBuffers(1, &_ebo);

	GLuint elements[] = {
		0, 1, 2,
		2, 3, 0
	};

	glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _ebo);
	glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(elements), elements, GL_STATIC_DRAW);

	// Shader sources
	const GLchar* vertexSource = R"glsl(
		#version 150 core
		in vec2 position;
		in vec2 texcoord;
		out vec2 Texcoord;
		void main()
		{
			Texcoord = texcoord;
			gl_Position = vec4(position, 0.0, 1.0);
		}
	)glsl";

	const GLchar* fragmentSource = R"glsl(
		#version 150 core
		in vec2 Texcoord;
		out vec4 outColor;
		uniform sampler2D tex;
		void main()
		{
			outColor = texture(tex, Texcoord);
		}
	)glsl";

	// Create and compile the vertex shader
	_vertexShader = glCreateShader(GL_VERTEX_SHADER);
	glShaderSource(_vertexShader, 1, &vertexSource, NULL);
	glCompileShader(_vertexShader);

	// Create and compile the fragment shader
	_fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
	glShaderSource(_fragmentShader, 1, &fragmentSource, NULL);
	glCompileShader(_fragmentShader);

	// Link the vertex and fragment shader into a shader program
	_shaderProgram = glCreateProgram();
	glAttachShader(_shaderProgram, _vertexShader);
	glAttachShader(_shaderProgram, _fragmentShader);
	glBindFragDataLocation(_shaderProgram, 0, "outColor");
	glLinkProgram(_shaderProgram);

	// Specify the layout of the vertex data
	GLint posAttrib = glGetAttribLocation(_shaderProgram, "position");
	glEnableVertexAttribArray(posAttrib);
	glVertexAttribPointer(posAttrib, 2, GL_FLOAT, GL_FALSE, 4 * sizeof(GLfloat), 0);

	GLint texAttrib = glGetAttribLocation(_shaderProgram, "texcoord");
	glEnableVertexAttribArray(texAttrib);
	glVertexAttribPointer(texAttrib, 2, GL_FLOAT, GL_FALSE, 4 * sizeof(GLfloat), (void*)(2 * sizeof(GLfloat)));
}

void Window::UnloadResources()
{
	glDeleteProgram(_shaderProgram);
	glDeleteShader(_fragmentShader);
	glDeleteShader(_vertexShader);
	glDeleteBuffers(1, &_ebo);
	glDeleteBuffers(1, &_vbo);
	glDeleteVertexArrays(1, &_vao);
}

bool Window::Render()
{
	if (_quit || _pWindow == nullptr)
	{
		return true;
	}
	
	SDL_Event event;
	while (SDL_PollEvent(&event) != 0)
	{		
		if (event.type == SDL_WINDOWEVENT)
		{
			switch (event.window.event)
			{
			case SDL_WINDOWEVENT_SIZE_CHANGED:
				_width = event.window.data1;
				_height = event.window.data2;
				_pTextureHandle = (GLuint)ResizeDelegate(this, _width, _height);
				break;
			}
		}
		else if (event.type == SDL_QUIT)
		{
			CloseDelegate(this);
			return false;
		}
	}

	wglMakeCurrent(_deviceContext, _unityContext);

	glBindVertexArray(_vao);
	glBindBuffer(GL_ARRAY_BUFFER, _vbo);
	glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _ebo);	

	glUseProgram(_shaderProgram);
	glBindTexture(GL_TEXTURE_2D, _pTextureHandle);
	glUniform1i(glGetUniformLocation(_shaderProgram, "tex"), 0);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

	glViewport(0, 0, _width, _height);
	glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0);

	SwapBuffers(_deviceContext);
	glFinish();

	return true;
}

Window::~Window()
{
	if (_pWindow == nullptr)
	{
		return;
	}

	//Destroy window
	SDL_DestroyWindow(_pWindow);
	_pWindow = nullptr;
}