    document.addEventListener('DOMContentLoaded', function () {
            const dropZone = document.getElementById('imageDropZone');
    const fileInput = document.getElementById('imageFileInput'); // Hidden input
    const imagePreview = document.getElementById('imagePreview');

    if (!dropZone || !fileInput || !imagePreview) {
        console.error("Required elements for drag-and-drop not found.");
    return;
            }

            // --- Trigger hidden file input click when drop zone is clicked ---
            dropZone.addEventListener('click', () => {
        fileInput.click();
            });

            // --- Handle file selection via browse button ---
            fileInput.addEventListener('change', (e) => {
                if (e.target.files && e.target.files.length > 0) {
        handleFile(e.target.files[0]);
                }
            });

            // --- Drag and Drop Event Handlers ---

            // Prevent default drag behaviors
            ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
    document.body.addEventListener(eventName, preventDefaults, false); // Prevent browser opening file
            });

    function preventDefaults(e) {
        e.preventDefault();
    e.stopPropagation();
            }

            // Highlight drop zone when item is dragged over it
            ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, highlight, false);
            });

            ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, unhighlight, false);
            });

    function highlight(e) {
        dropZone.classList.add('dragover-active');
            }

    function unhighlight(e) {
        dropZone.classList.remove('dragover-active');
            }

    // Handle dropped files
    dropZone.addEventListener('drop', handleDrop, false);

    function handleDrop(e) {
                const dt = e.dataTransfer;
    const files = dt.files;

                if (files.length > 0) {
        handleFile(files[0]);
    // Update the hidden file input's files collection
    // Create a new FileList if necessary as input.files is read-only
    try {
        fileInput.files = files;
    // Trigger change event manually if needed for validation frameworks
    fileInput.dispatchEvent(new Event('change', {bubbles: true }));
                     } catch (err) {
        console.error("Error setting file input files:", err);
                         // Fallback for some browsers might be needed if direct assignment fails
                     }
                } else {
        console.log("No file dropped?");
                }
            }

    // --- Shared File Handling Logic (Validation & Preview) ---
    function handleFile(file) {
        // Reset preview and errors
        imagePreview.style.display = 'none';
    imagePreview.src = '#';
    // TODO: Clear any previous custom validation messages

    if (!file) {
        // No file selected/dropped
        fileInput.value = ''; // Clear the input
    return;
                }

    // Validate file type (basic image check)
    if (!file.type.startsWith('image/')) {
        alert('Invalid file type. Please upload an image (PNG, JPG, GIF).');
    fileInput.value = ''; // Clear the input
    return;
                }

    // Validate file size (e.g., max 5MB)
    const maxSizeMB = 5;
                if (file.size > maxSizeMB * 1024 * 1024) {
        alert(`File is too large. Maximum size is ${maxSizeMB} MB.`);
    fileInput.value = ''; // Clear the input
    return;
                }

    // If using fileInput directly (not drop), its files property is already set
    // If using drop, we already set fileInput.files in handleDrop

    // Show preview
    const reader = new FileReader();
    reader.onload = function (e) {
        imagePreview.src = e.target.result;
    imagePreview.style.display = 'block';
                }
    reader.readAsDataURL(file);
    console.log("File handled and preview requested:", file.name);
            }
        });
