// File download utilities for Blazor applications

window.downloadFileFromBytes = (fileName, bytes) => {
    try {
        // Convert byte array to blob
        const blob = new Blob([bytes], { type: 'text/csv;charset=utf-8;' });
        
        // Create download link
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        
        link.setAttribute('href', url);
        link.setAttribute('download', fileName);
        link.style.visibility = 'hidden';
        
        // Add to DOM, click, and remove
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        // Clean up the URL object
        URL.revokeObjectURL(url);
        
        return true;
    } catch (error) {
        console.error('Error downloading file:', error);
        return false;
    }
};

window.downloadFile = (fileName, content) => {
    try {
        // Create blob from content
        const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
        
        // Create download link
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        
        link.setAttribute('href', url);
        link.setAttribute('download', fileName);
        link.style.visibility = 'hidden';
        
        // Add to DOM, click, and remove
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        // Clean up the URL object
        URL.revokeObjectURL(url);
        
        return true;
    } catch (error) {
        console.error('Error downloading file:', error);
        return false;
    }
};

// Generic file download function
window.downloadFileGeneric = (fileName, content, mimeType = 'application/octet-stream') => {
    try {
        const blob = new Blob([content], { type: mimeType });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        
        link.setAttribute('href', url);
        link.setAttribute('download', fileName);
        link.style.visibility = 'hidden';
        
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        URL.revokeObjectURL(url);
        
        return true;
    } catch (error) {
        console.error('Error downloading file:', error);
        return false;
    }
};
