#pragma once

#include <string>

std::string GetLastErrorAsString(unsigned long errorMessageID);
void ErrorCheck(const std::string& event);