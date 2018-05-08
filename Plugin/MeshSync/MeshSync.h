#pragma once

#include "MeshUtils/MeshUtils.h"
#include "msConfig.h"
#include "msConstraints.h"
#include "msSceneGraph.h"
#include "msClient.h"
#include "msServer.h"

#ifdef mscDebug
    #define mscTrace(...) ::mu::Print("MeshSync trace: " __VA_ARGS__)
#else
    #define mscTrace(...)
#endif
