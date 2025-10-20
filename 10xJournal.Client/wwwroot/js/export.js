/**
 * Downloads a file with the given filename and content.
 * Creates a temporary Blob URL and triggers a download in the browser.
 * 
 * @param {string} filename - The name of the file to download
 * @param {string} content - The content of the file (as a string)
 */
window.downloadFile = (filename, content) => {
    // Create a Blob from the content
    const blob = new Blob([content], { type: 'application/json' });
    
    // Create a temporary URL for the Blob
    const url = URL.createObjectURL(blob);
    
    // Create a temporary anchor element and trigger the download
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.style.display = 'none';
    
    document.body.appendChild(anchor);
    anchor.click();
    
    // Clean up
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
};
