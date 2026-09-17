// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}

export function drawRectangle(canvasId, imageData, x, y, width, height) {
    try {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('Canvas not found:', canvasId);
            return;
        }
        
        const ctx = canvas.getContext('2d');
        
        // Create ImageData from the provided data
        const imgData = new ImageData(new Uint8ClampedArray(imageData), width, height);
        
        // Draw the image data at the specified position
        ctx.putImageData(imgData, x, y);
    } catch (error) {
        console.error('Error drawing rectangle:', error);
    }
}

export function getDimensions() {
    return [window.innerWidth, window.innerHeight];
}

export function getCanvasDimensions(canvasId) {
    try {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return { width: 800, height: 600 }; // Default size
        }
        
        // Get the actual displayed size (which may be scaled by CSS)
        const rect = canvas.getBoundingClientRect();
        return { 
            width: rect.width, 
            height: rect.height,
            // Also include the internal canvas dimensions for reference
            canvasWidth: canvas.width,
            canvasHeight: canvas.height
        };
    } catch (error) {
        console.error('Error getting canvas dimensions:', error);
        return { width: 800, height: 600 };
    }
}

export function setCanvasSize(canvasId, width, height) {
    try {
        const canvas = document.getElementById(canvasId);
        if (canvas) {
            canvas.width = width;
            canvas.height = height;
        }
    } catch (error) {
        console.error('Error setting canvas size:', error);
    }
}

export function clearCanvas(canvasId) {
    try {
        const canvas = document.getElementById(canvasId);
        if (canvas) {
            const ctx = canvas.getContext('2d');
            ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
    } catch (error) {
        console.error('Error clearing canvas:', error);
    }
}

// Fullscreen API functions
export function enterFullscreen(elementId) {
    try {
        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Element not found:', elementId);
            return false;
        }

        if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.webkitRequestFullscreen) { // Safari
            element.webkitRequestFullscreen();
        } else if (element.msRequestFullscreen) { // IE/Edge
            element.msRequestFullscreen();
        } else if (element.mozRequestFullScreen) { // Firefox
            element.mozRequestFullScreen();
        } else {
            console.error('Fullscreen API not supported');
            return false;
        }
        return true;
    } catch (error) {
        console.error('Error entering fullscreen:', error);
        return false;
    }
}

export function exitFullscreen() {
    try {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        } else if (document.webkitExitFullscreen) { // Safari
            document.webkitExitFullscreen();
        } else if (document.msExitFullscreen) { // IE/Edge
            document.msExitFullscreen();
        } else if (document.mozCancelFullScreen) { // Firefox
            document.mozCancelFullScreen();
        } else {
            console.error('Exit fullscreen API not supported');
            return false;
        }
        return true;
    } catch (error) {
        console.error('Error exiting fullscreen:', error);
        return false;
    }
}

export function isFullscreen() {
    return !!(document.fullscreenElement || 
              document.webkitFullscreenElement || 
              document.msFullscreenElement || 
              document.mozFullScreenElement);
}

export function addFullscreenChangeListener(dotNetObjectRef, methodName) {
    const handleFullscreenChange = () => {
        const isNowFullscreen = isFullscreen();
        dotNetObjectRef.invokeMethodAsync(methodName, isNowFullscreen);
    };

    // Store the handler for cleanup
    if (!window._fullscreenHandler) {
        window._fullscreenHandler = handleFullscreenChange;
        
        // Add listeners for all browser prefixes
        document.addEventListener('fullscreenchange', handleFullscreenChange);
        document.addEventListener('webkitfullscreenchange', handleFullscreenChange);
        document.addEventListener('msfullscreenchange', handleFullscreenChange);
        document.addEventListener('mozfullscreenchange', handleFullscreenChange);
    }
}

export function removeFullscreenChangeListener() {
    if (window._fullscreenHandler) {
        const handler = window._fullscreenHandler;
        document.removeEventListener('fullscreenchange', handler);
        document.removeEventListener('webkitfullscreenchange', handler);
        document.removeEventListener('msfullscreenchange', handler);
        document.removeEventListener('mozfullscreenchange', handler);
        window._fullscreenHandler = null;
    }
}
