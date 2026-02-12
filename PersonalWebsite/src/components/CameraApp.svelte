<script>
    import { onMount, onDestroy } from "svelte";

    let video;
    let canvas;
    let stream;
    let facingMode = "environment";
    let error = null;
    let torchSupported = false;
    let torchEnabled = false;
    let pendingUploads = 0;
    let isFlashing = false;
    let lastUploadStatus = "";
    let nextCaptureField = "patientInfoImage";
    let queuedPatientInfoBlob = null;
    let queuedOperationBlob = null;

    // OCR & Review Mode State
    let cvLoaded = false;
    let scannerLoaded = false;
    let isReviewing = false;
    let isCropping = false; // New state for cropping
    let reviewModeEnabled = false; // Toggle for "Review every shot"
    let isProcessing = false;
    let resultCanvas; // Canvas for the processed result
    let capturedImage = null; // Stores the current working image (may be cropped)
    let originalImage = null; // Stores the raw captured image for re-cropping
    let processedBlob = null; // The blob ready to upload
    let processedBlobCaptureId = 0;
    let processedBlobField = null;
    let captureSequence = 0;
    let activeCaptureId = 0;
    let activeCaptureField = null;
    let isCapturing = false;
    let scanner; // jscanify instance
    let cropper; // Cropper.js instance

    const API_BASE_URL = "https://api.ayanamikaine.com";
    const API_PASSWORD = "SecretPassword123";
    const MAX_UPLOAD_ATTEMPTS = 20;
    const BASE_RETRY_DELAY_MS = 1500;
    const MAX_RETRY_DELAY_MS = 15000;
    let uploadQueue = [];
    let uploadQueueSize = 0;
    let isUploadWorkerRunning = false;
    let showMetadataPanel = false;
    let metadataOperateur = "";
    let metadataNamePatient = "";
    let metadataGeburtsdatum = "";
    let metadataKnownFields = [];
    let metadataExtras = [];
    let metadataPayload = {};
    let metadataFieldCount = 0;

    const OPERATEUR_OPTIONS = [
        "Menzel-Severing",
        "Steindor",
        "Borrelli",
        "Gerling",
    ];

    const METADATA_FIELD_GROUPS = [
        {
            section: "Spender / Präparation",
            fields: [
                { key: "sp_nummer", label: "SP-Nummer", type: "string" },
                {
                    key: "spender_alter",
                    label: "Spender Alter",
                    type: "string",
                },
                { key: "alter", label: "Alter", type: "integer" },
                {
                    key: "pseudophak_spender",
                    label: "Pseudophak (Spender)",
                    type: "string",
                    options: ["Ja", "Nein"],
                },
                {
                    key: "korneoskleralscheibe_zentriert",
                    label: "Korneoskleralscheibe zentriert",
                    type: "string",
                    options: ["Ja", "Nein"],
                },
                {
                    key: "datum_praeparation",
                    label: "Datum Präparation",
                    type: "date",
                },
                { key: "dauer_min", label: "Dauer (min)", type: "integer" },
                {
                    key: "tmw_pigmentierung",
                    label: "TMW-Pigmentierung",
                    type: "string",
                    options: ["0", "+", "++", "+++"],
                },
                {
                    key: "nach_praep_uhrzeiten",
                    label: "Nach Präp. Uhrzeiten",
                    type: "string",
                },
                {
                    key: "dmek_praeparat_durchmesser",
                    label: "DMEK-Durchmesser",
                    type: "string",
                },
                {
                    key: "no_touch_technik",
                    label: "No-touch-Technik",
                    type: "string",
                    options: ["Ja", "Nein"],
                },
                {
                    key: "leichtigkeit_praeparation",
                    label: "Leichtigkeit Präparation",
                    type: "string",
                    options: ["Leicht", "Mittel", "Schwer"],
                },
                {
                    key: "grund_schwierigkeit_praep",
                    label: "Grund Schwierigkeit Präp.",
                    type: "string",
                },
                {
                    key: "risse_defekte",
                    label: "Risse/Defekte",
                    type: "string",
                },
                {
                    key: "faerbung_praeparat",
                    label: "Färbung Präparat",
                    type: "string",
                },
                {
                    key: "tx_endotheldefekte",
                    label: "Tx-Endotheldefekte",
                    type: "string",
                    options: ["0", "+", "++", "+++"],
                },
                {
                    key: "streifen_punkte",
                    label: "Streifen/Punkte",
                    type: "string",
                    options: ["Streifen", "Punkte"],
                },
                {
                    key: "dauer_trypanblau_sek",
                    label: "Dauer Trypanblau (s)",
                    type: "integer",
                },
                {
                    key: "lagerung_bis_tx",
                    label: "Lagerung bis Tx",
                    type: "string",
                },
                {
                    key: "rollung",
                    label: "Rollung",
                    type: "string",
                    options: ["Normal", "Eng", "Kaum"],
                },
                { key: "markierung", label: "Markierung", type: "string" },
            ],
        },
        {
            section: "Empfänger / Operation",
            fields: [
                {
                    key: "op_auge",
                    label: "Operiertes Auge",
                    type: "string",
                    options: ["RA", "LA"],
                },
                {
                    key: "pseudophak_empfaenger",
                    label: "Pseudophak (Empfänger)",
                    type: "string",
                    options: ["Ja", "Nein"],
                },
                {
                    key: "hornhaut_durchmesser.vertikal",
                    label: "Hornhaut vertikal (mm)",
                    type: "number",
                },
                {
                    key: "hornhaut_durchmesser.horizontal",
                    label: "Hornhaut horizontal (mm)",
                    type: "number",
                },
                { key: "datum_op", label: "Datum OP", type: "date" },
                {
                    key: "beginn_zeit",
                    label: "Beginn Uhrzeit",
                    type: "string",
                    inputType: "time",
                },
                {
                    key: "gesamtdauer_min",
                    label: "Gesamtdauer (min)",
                    type: "integer",
                },
                {
                    key: "abrasio",
                    label: "Abrasio",
                    type: "string",
                    options: ["Ja", "Nein"],
                },
                { key: "faerbung_op", label: "Färbung OP", type: "string" },
                {
                    key: "descemetorhexis_zentriert",
                    label: "Descemetorhexis zentriert",
                    type: "string",
                    options: ["Ja", "Nein"],
                },
                {
                    key: "trypanblau_applikation_vor_implant",
                    label: "Trypanblau vor Implant (s)",
                    type: "integer",
                },
                {
                    key: "implantation_szurmann",
                    label: "Implantation Szurmann",
                    type: "string",
                },
                {
                    key: "leichtigkeit_entrollung",
                    label: "Leichtigkeit Entrollung",
                    type: "string",
                    options: ["Leicht", "Mittel", "Schwer"],
                },
                {
                    key: "grund_schwierigkeit_entrollung",
                    label: "Grund Schwierigkeit Entrollung",
                    type: "string",
                },
                {
                    key: "entfaltung",
                    label: "Entfaltung",
                    type: "string",
                    options: [
                        "Spontan",
                        "Manuelle Hilfe",
                        "Mit kleiner Luftblase",
                    ],
                },
                {
                    key: "blutung_in_vk",
                    label: "Blutung in VK",
                    type: "string",
                    options: ["Ja", "Nein"],
                },
                {
                    key: "trypanblau_intraokular_sek",
                    label: "Trypanblau intraokular (s)",
                    type: "integer",
                },
                {
                    key: "kat_op",
                    label: "Katarakt-OP",
                    type: "string",
                    options: ["Ja", "Nein"],
                },
                {
                    key: "iridektomie_uhrzeit",
                    label: "Iridektomie (Uhrzeit)",
                    type: "string",
                },
                {
                    key: "naehte.typ",
                    label: "Nähte Typ",
                    type: "string",
                    options: ["Keine", "Kreuz", "EKN"],
                },
                {
                    key: "naehte.uhrzeit",
                    label: "Nähte Uhrzeit",
                    type: "string",
                },
                {
                    key: "luftblase_prozent",
                    label: "Luftblase (%)",
                    type: "integer",
                },
                {
                    key: "gas_luft_typ",
                    label: "Gas/Luft Typ",
                    type: "string",
                    options: ["SF6 20%", "Luft"],
                },
                {
                    key: "komplikationen",
                    label: "Komplikationen",
                    type: "string",
                },
                { key: "kommentare", label: "Kommentare", type: "string" },
            ],
        },
    ];

    const METADATA_FIELD_CATALOG = METADATA_FIELD_GROUPS.flatMap((group) =>
        group.fields.map((field) => ({
            ...field,
            section: group.section,
        })),
    );

    const METADATA_FIELD_BY_KEY = METADATA_FIELD_CATALOG.reduce(
        (acc, field) => {
            acc[field.key] = field;
            return acc;
        },
        {},
    );

    // Processing Parameters
    let filters = {
        type: "bw", // 'original', 'gray', 'bw'
        brightness: 0,
        contrast: 1.0,
        blockSize: 31,
        threshold: 12,
        denoise: 1,
        denoise: 1,
        autoCrop: false, // Default to false now
    };

    const libraries = {
        opencv: "https://docs.opencv.org/4.7.0/opencv.js",
        jscanify:
            "https://cdn.jsdelivr.net/gh/ColonelParrot/jscanify@master/src/jscanify.min.js",
        cropperjs:
            "https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.6.1/cropper.min.js",
    };

    // --- Persistence ---
    function loadSettings() {
        try {
            const savedFilters = localStorage.getItem("cameraApp_filters");
            const savedReviewMode = localStorage.getItem(
                "cameraApp_reviewMode",
            );

            if (savedFilters) {
                filters = { ...filters, ...JSON.parse(savedFilters) };
            }
            if (savedReviewMode !== null) {
                reviewModeEnabled = JSON.parse(savedReviewMode);
            }
        } catch (e) {
            console.error("Failed to load settings", e);
        }
    }

    function saveSettings() {
        try {
            localStorage.setItem("cameraApp_filters", JSON.stringify(filters));
            localStorage.setItem(
                "cameraApp_reviewMode",
                JSON.stringify(reviewModeEnabled),
            );
        } catch (e) {
            console.error("Failed to save settings", e);
        }
    }

    function loadMetadataDraft() {
        try {
            const raw = localStorage.getItem("cameraApp_metadataDraft");
            if (!raw) return;
            const saved = JSON.parse(raw);

            metadataOperateur =
                typeof saved.operateur === "string" ? saved.operateur : "";
            metadataNamePatient =
                typeof saved.name_patient === "string"
                    ? saved.name_patient
                    : "";
            metadataGeburtsdatum =
                typeof saved.geburtsdatum === "string"
                    ? saved.geburtsdatum
                    : "";

            if (Array.isArray(saved.knownFields)) {
                metadataKnownFields = saved.knownFields
                    .filter(
                        (item) =>
                            item &&
                            typeof item.key === "string" &&
                            typeof item.value === "string",
                    )
                    .map((item) => ({ key: item.key, value: item.value }));
            } else {
                metadataKnownFields = [];
            }

            if (Array.isArray(saved.extras)) {
                metadataExtras = saved.extras
                    .filter(
                        (item) =>
                            item &&
                            typeof item.key === "string" &&
                            typeof item.value === "string",
                    )
                    .map((item) => ({ key: item.key, value: item.value }));
            } else {
                metadataExtras = [];
            }
        } catch (e) {
            console.error("Failed to load metadata draft", e);
        }
    }

    function saveMetadataDraft() {
        try {
            localStorage.setItem(
                "cameraApp_metadataDraft",
                JSON.stringify({
                    operateur: metadataOperateur,
                    name_patient: metadataNamePatient,
                    geburtsdatum: metadataGeburtsdatum,
                    knownFields: metadataKnownFields,
                    extras: metadataExtras,
                }),
            );
        } catch (e) {
            console.error("Failed to save metadata draft", e);
        }
    }

    function getMetadataFieldDefinition(key) {
        return METADATA_FIELD_BY_KEY[key] ?? null;
    }

    function getMetadataInputType(key) {
        const definition = getMetadataFieldDefinition(key);
        if (!definition) return "text";
        if (definition.inputType) return definition.inputType;
        if (definition.type === "integer" || definition.type === "number") {
            return "number";
        }
        if (definition.type === "date") return "date";
        return "text";
    }

    function getMetadataInputStep(key) {
        const definition = getMetadataFieldDefinition(key);
        if (!definition) return null;
        if (definition.type === "integer") return "1";
        if (definition.type === "number") return "0.1";
        return null;
    }

    function getMetadataInputMode(key) {
        const definition = getMetadataFieldDefinition(key);
        if (!definition) return null;
        if (definition.type === "integer") return "numeric";
        if (definition.type === "number") return "decimal";
        return null;
    }

    function setNestedMetadataValue(target, key, value) {
        const path = key.split(".");
        let cursor = target;

        for (let i = 0; i < path.length - 1; i++) {
            const part = path[i];
            if (
                typeof cursor[part] !== "object" ||
                cursor[part] === null ||
                Array.isArray(cursor[part])
            ) {
                cursor[part] = {};
            }
            cursor = cursor[part];
        }

        cursor[path[path.length - 1]] = value;
    }

    function normalizeKnownMetadataValue(rawValue, definition) {
        const rawText = `${rawValue ?? ""}`.trim();
        if (!rawText) return null;

        if (
            Array.isArray(definition.options) &&
            definition.options.length > 0 &&
            !definition.options.includes(rawText)
        ) {
            return null;
        }

        if (definition.type === "integer") {
            const parsed = Number.parseInt(rawText, 10);
            return Number.isNaN(parsed) ? null : parsed;
        }

        if (definition.type === "number") {
            const parsed = Number.parseFloat(rawText.replace(",", "."));
            return Number.isNaN(parsed) ? null : parsed;
        }

        return rawText;
    }

    function buildMetadataPayload() {
        const metadata = {};

        for (const field of metadataKnownFields) {
            const key = (field.key ?? "").trim();
            if (!key) continue;
            const definition = getMetadataFieldDefinition(key);
            if (!definition) continue;

            const normalized = normalizeKnownMetadataValue(
                field.value,
                definition,
            );
            if (normalized === null) continue;
            setNestedMetadataValue(metadata, key, normalized);
        }

        for (const pair of metadataExtras) {
            const key = (pair.key ?? "").trim();
            const value = (pair.value ?? "").trim();
            if (!key || !value) continue;
            setNestedMetadataValue(metadata, key, value);
        }

        // Core fields are quick-access and should win over duplicates.
        const operateur = metadataOperateur.trim();
        const namePatient = metadataNamePatient.trim();
        const geburtsdatum = metadataGeburtsdatum.trim();

        if (operateur) metadata.operateur = operateur;
        if (namePatient) metadata.name_patient = namePatient;
        if (geburtsdatum) metadata.geburtsdatum = geburtsdatum;

        return metadata;
    }

    function openMetadataPanel() {
        showMetadataPanel = true;
    }

    function closeMetadataPanel() {
        showMetadataPanel = false;
        saveMetadataDraft();
    }

    function setMetadataField(field, value) {
        if (field === "operateur") metadataOperateur = value;
        if (field === "name_patient") metadataNamePatient = value;
        if (field === "geburtsdatum") metadataGeburtsdatum = value;
        saveMetadataDraft();
    }

    function addMetadataExtra() {
        metadataExtras = [...metadataExtras, { key: "", value: "" }];
        saveMetadataDraft();
    }

    function addKnownMetadataField() {
        metadataKnownFields = [...metadataKnownFields, { key: "", value: "" }];
        saveMetadataDraft();
    }

    function updateKnownMetadataField(index, field, value) {
        metadataKnownFields = metadataKnownFields.map((entry, i) => {
            if (i !== index) return entry;
            if (field === "key") {
                return { key: value, value: "" };
            }
            return { ...entry, [field]: value };
        });
        saveMetadataDraft();
    }

    function removeKnownMetadataField(index) {
        metadataKnownFields = metadataKnownFields.filter((_, i) => i !== index);
        saveMetadataDraft();
    }

    function updateMetadataExtra(index, field, value) {
        metadataExtras = metadataExtras.map((entry, i) =>
            i === index ? { ...entry, [field]: value } : entry,
        );
        saveMetadataDraft();
    }

    function removeMetadataExtra(index) {
        metadataExtras = metadataExtras.filter((_, i) => i !== index);
        saveMetadataDraft();
    }

    function clearMetadataDraft() {
        metadataOperateur = "";
        metadataNamePatient = "";
        metadataGeburtsdatum = "";
        metadataKnownFields = [];
        metadataExtras = [];
        saveMetadataDraft();
    }

    $: metadataPayload = buildMetadataPayload();
    $: metadataFieldCount = Object.keys(metadataPayload).length;
    $: captureDisabled =
        isCapturing ||
        isProcessing ||
        isCropping ||
        isReviewing;
    $: saveDisabled =
        isProcessing ||
        !processedBlob ||
        processedBlobCaptureId !== activeCaptureId ||
        processedBlobField !== activeCaptureField;

    async function loadScript(src, checkVar) {
        return new Promise((resolve, reject) => {
            if (window[checkVar]) {
                resolve();
                return;
            }
            const script = document.createElement("script");
            script.src = src;
            script.async = true;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    function loadCSS(href) {
        if (document.querySelector(`link[href="${href}"]`)) return;
        const link = document.createElement("link");
        link.href = href;
        link.rel = "stylesheet";
        document.head.appendChild(link);
    }

    async function initLibraries() {
        try {
            // Load Cropper CSS
            loadCSS(
                "https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.6.1/cropper.min.css",
            );

            await loadScript(libraries.jscanify, "jscanify");
            scannerLoaded = true;
            console.log("jscanify loaded");

            await loadScript(libraries.cropperjs, "Cropper");
            console.log("Cropper loaded");

            // Initialize scanner if loaded
            if (typeof jscanify !== "undefined") {
                scanner = new jscanify();
            }

            // OpenCV takes a bit longer and sets 'cv'
            await loadScript(libraries.opencv, "cv");
            // Wait for cv to be fully ready
            const checkCv = setInterval(() => {
                if (typeof cv !== "undefined" && cv.Mat) {
                    clearInterval(checkCv);
                    cvLoaded = true;
                    console.log("OpenCV loaded");
                }
            }, 100);
        } catch (e) {
            console.error("Failed to load libraries", e);
            error = "Failed to load image processing libraries.";
        }
    }

    async function startCamera() {
        try {
            if (stream) {
                stream.getTracks().forEach((track) => track.stop());
            }

            // Check for hardware stabilization support
            const supported = navigator.mediaDevices.getSupportedConstraints();
            const stabilization = supported.imageStabilization || false;

            // Initial request: Ask for high resolution + stabilization
            const constraints = {
                video: {
                    facingMode: facingMode,
                    width: { ideal: 4096 },
                    height: { ideal: 2160 },
                    // Request stabilization if supported (browser might ignore if not 'exact', but 'ideal' is safer)
                    ...(stabilization ? { imageStabilization: true } : {}),
                },
            };

            try {
                stream = await navigator.mediaDevices.getUserMedia(constraints);
            } catch (e) {
                console.warn(
                    "Specific facingMode failed, falling back to any video camera",
                    e,
                );
                // Fallback: remove facingMode constraint
                delete constraints.video.facingMode;
                stream = await navigator.mediaDevices.getUserMedia(constraints);
            }

            // Optimization: dynamic resolution scaling based on capabilities
            const track = stream.getVideoTracks()[0];
            const capabilities = track.getCapabilities
                ? track.getCapabilities()
                : {};

            console.log("Camera Capabilities:", capabilities);

            // If the camera reports resolution capabilities, try to maximize them
            if (capabilities.width && capabilities.height) {
                const maxWidth = capabilities.width.max;
                const maxHeight = capabilities.height.max;

                // Only apply if they are significantly different/better than what we might have got (or just force max)
                // We apply 'ideal' max to ensure we get the best possible quality
                try {
                    await track.applyConstraints({
                        ...constraints.video,
                        width: { ideal: maxWidth },
                        height: { ideal: maxHeight },
                    });
                    console.log(
                        `Applied max resolution: ${maxWidth}x${maxHeight}`,
                    );
                } catch (err) {
                    console.warn(
                        "Failed to apply max resolution constraints:",
                        err,
                    );
                }
            }

            video.srcObject = stream;
            video.play();

            // Check for torch support
            torchSupported = !!capabilities.torch;
            torchEnabled = false; // Reset state

            error = null;
        } catch (err) {
            console.error("Error accessing camera:", err);
            error =
                "Could not access the camera. Please ensure you have granted permissions.";
        }
    }

    function switchCamera() {
        facingMode = facingMode === "user" ? "environment" : "user";
        startCamera();
    }

    async function toggleTorch() {
        if (!stream) return;
        const track = stream.getVideoTracks()[0];

        try {
            await track.applyConstraints({
                advanced: [{ torch: !torchEnabled }],
            });
            torchEnabled = !torchEnabled;
        } catch (err) {
            console.error("Error toggling torch:", err);
        }
    }

    function getCaptureStepLabel() {
        return nextCaptureField === "patientInfoImage"
            ? "Step 1/2: Patient Info"
            : "Step 2/2: Operation Details";
    }

    function getCaptureStepHint() {
        return nextCaptureField === "patientInfoImage"
            ? "Capture name, birth date, and surgeon fields."
            : "Capture the operation section fields.";
    }

    function beginCaptureSession() {
        if (captureDisabled) return null;

        if (nextCaptureField === "operationImage" && !queuedPatientInfoBlob) {
            nextCaptureField = "patientInfoImage";
            error = "First image is missing. Capture and save page 1 first.";
            return null;
        }

        captureSequence += 1;
        activeCaptureId = captureSequence;
        activeCaptureField = nextCaptureField;
        processedBlob = null;
        processedBlobCaptureId = 0;
        processedBlobField = null;
        isCapturing = true;
        error = null;

        return {
            captureId: activeCaptureId,
            targetField: activeCaptureField,
        };
    }

    function clearCaptureBuffer() {
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }
        isReviewing = false;
        isCropping = false;
        isProcessing = false;
        isCapturing = false;
        capturedImage = null;
        originalImage = null;
        processedBlob = null;
        processedBlobCaptureId = 0;
        processedBlobField = null;
        activeCaptureId = 0;
        activeCaptureField = null;
    }

    function resetUploadSequence() {
        nextCaptureField = "patientInfoImage";
        queuedPatientInfoBlob = null;
        queuedOperationBlob = null;
    }

    function shouldRetryStatus(status) {
        if (status === 408 || status === 429) return true;
        if (status >= 500) return true;
        return false;
    }

    function getRetryDelayMs(attempt) {
        const delay = BASE_RETRY_DELAY_MS * attempt;
        return Math.min(delay, MAX_RETRY_DELAY_MS);
    }

    function queueUploadJob(patientInfoBlob, operationBlob, metadata = null) {
        if (!patientInfoBlob || !operationBlob) {
            error = "Both images are required before upload.";
            return;
        }

        const jobId = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
        const job = {
            id: jobId,
            patientInfoBlob,
            operationBlob,
            metadata:
                metadata && Object.keys(metadata).length > 0
                    ? { ...metadata }
                    : null,
        };

        uploadQueue = [...uploadQueue, job];
        uploadQueueSize = uploadQueue.length;
        const pendingPairs =
            uploadQueueSize + pendingUploads + (isUploadWorkerRunning ? 1 : 0);
        lastUploadStatus = `Queued upload (${pendingPairs} pending).`;
        processUploadQueue();
    }

    async function uploadPairOnce(job, attempt) {
        pendingUploads++;

        const formData = new FormData();
        formData.append(
            "patientInfoImage",
            job.patientInfoBlob,
            `patient-info-${job.id}.jpg`,
        );
        formData.append(
            "operationImage",
            job.operationBlob,
            `operation-${job.id}.jpg`,
        );
        if (job.metadata && Object.keys(job.metadata).length > 0) {
            formData.append("metadata", JSON.stringify(job.metadata));
        }

        try {
            const response = await fetch(`${API_BASE_URL}/upload`, {
                method: "POST",
                headers: {
                    "X-Password": API_PASSWORD,
                },
                body: formData,
            });

            if (response.ok) {
                const result = await response.json().catch(() => null);
                error = null;
                lastUploadStatus = result?.jobId
                    ? `Images queued. Job ID: ${result.jobId}`
                    : "Upload successful";
                return { success: true, retryable: false };
            }

            console.error(
                `Upload failed (attempt ${attempt}/${MAX_UPLOAD_ATTEMPTS}):`,
                response.status,
                response.statusText,
            );
            return {
                success: false,
                retryable: shouldRetryStatus(response.status),
                status: response.status,
                reason: response.statusText,
            };
        } catch (err) {
            console.error(
                `Upload network error (attempt ${attempt}/${MAX_UPLOAD_ATTEMPTS}):`,
                err,
            );
            return {
                success: false,
                retryable: true,
                reason: "Network error",
            };
        } finally {
            pendingUploads--;
        }
    }

    async function uploadJobWithRetry(job) {
        for (let attempt = 1; attempt <= MAX_UPLOAD_ATTEMPTS; attempt++) {
            const result = await uploadPairOnce(job, attempt);
            if (result.success) {
                return true;
            }

            if (!result.retryable) {
                error = `Upload rejected (${result.status ?? "unknown"}).`;
                lastUploadStatus = `Upload rejected (${result.status ?? "unknown"}).`;
                return false;
            }

            if (attempt < MAX_UPLOAD_ATTEMPTS) {
                const delayMs = getRetryDelayMs(attempt);
                const seconds = Math.ceil(delayMs / 1000);
                lastUploadStatus = `Upload failed (${attempt}/${MAX_UPLOAD_ATTEMPTS}), retrying in ${seconds}s...`;
                await wait(delayMs);
            }
        }

        error = `Upload failed after ${MAX_UPLOAD_ATTEMPTS} attempts; that pair was skipped.`;
        lastUploadStatus = `Dropped one upload after ${MAX_UPLOAD_ATTEMPTS} failed attempts.`;
        return false;
    }

    async function processUploadQueue() {
        if (isUploadWorkerRunning) return;
        isUploadWorkerRunning = true;

        try {
            while (uploadQueue.length > 0) {
                const [job, ...remainingJobs] = uploadQueue;
                uploadQueue = remainingJobs;
                uploadQueueSize = uploadQueue.length;
                await uploadJobWithRetry(job);
            }
        } finally {
            isUploadWorkerRunning = false;
            if (uploadQueue.length > 0) {
                processUploadQueue();
                return;
            }
            if (pendingUploads === 0 && uploadQueue.length === 0) {
                setTimeout(() => {
                    if (
                        pendingUploads === 0 &&
                        uploadQueue.length === 0 &&
                        !isUploadWorkerRunning
                    ) {
                        lastUploadStatus = "";
                    }
                }, 2500);
            }
        }
    }

    function triggerFlash() {
        isFlashing = true;
        setTimeout(() => {
            isFlashing = false;
        }, 150);
    }

    async function capturePhoto() {
        if (!video || !canvas) return;
        const session = beginCaptureSession();
        if (!session) return;
        const { captureId, targetField } = session;

        triggerFlash();

        // Use Smart Capture (Burst) if OpenCV is available to find the sharpest image
        if (cvLoaded && cv.Mat) {
            try {
                isProcessing = true;
                const bestImage = await captureBurst(3, captureId); // Capture 3 frames
                if (captureId !== activeCaptureId) return;
                if (bestImage) {
                    finalizeCapture(bestImage, captureId, targetField);
                } else {
                    // Fallback to single capture
                    captureSingleFrame(captureId, targetField);
                }
            } catch (e) {
                console.error("Smart capture failed, falling back", e);
                if (captureId === activeCaptureId) {
                    captureSingleFrame(captureId, targetField);
                }
            } finally {
                if (captureId === activeCaptureId) {
                    isProcessing = false;
                }
            }
        } else {
            captureSingleFrame(captureId, targetField);
        }
    }

    function captureSingleFrame(captureId, targetField) {
        if (!video || !canvas) return;
        const context = canvas.getContext("2d");
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        const img = new Image();
        img.onload = () => {
            if (captureId !== activeCaptureId) return;
            finalizeCapture(img, captureId, targetField);
        };
        img.src = canvas.toDataURL("image/jpeg");
    }

    function finalizeCapture(img, captureId, targetField) {
        if (captureId !== activeCaptureId || targetField !== activeCaptureField) {
            return;
        }

        capturedImage = img;
        originalImage = img; // Save original

        // If libraries are ready
        if (cvLoaded && scannerLoaded) {
            if (reviewModeEnabled) {
                isProcessing = false;
                startCrop(captureId); // Go to crop mode first
            } else {
                // Burst mode: skip crop, straight to process
                processImage(true, captureId, targetField);
            }
        } else {
            // Fallback direct upload if libs NOT ready
            const fallbackCanvas = document.createElement("canvas");
            fallbackCanvas.width = img.width;
            fallbackCanvas.height = img.height;
            fallbackCanvas.getContext("2d").drawImage(img, 0, 0);

            fallbackCanvas.toBlob(
                (blob) => {
                    if (captureId !== activeCaptureId) return;
                    if (!blob) {
                        error = "Failed to prepare image. Please retake.";
                        clearCaptureBuffer();
                        return;
                    }
                    processedBlob = blob;
                    processedBlobCaptureId = captureId;
                    processedBlobField = targetField;
                    uploadProcessed(captureId, targetField);
                },
                "image/jpeg",
                0.85,
            );
        }
    }

    // --- Smart Capture Logic ---

    function wait(ms) {
        return new Promise((resolve) => setTimeout(resolve, ms));
    }

    async function captureBurst(frameCount = 3, captureId = activeCaptureId) {
        console.log("Starting Smart Burst Capture...");
        let bestScore = -1;
        let bestImg = null;

        // Create a temporary canvas for analysis resizing (speed up cv.Laplacian)
        const analysisCanvas = document.createElement("canvas");
        const ctx = analysisCanvas.getContext("2d");
        // Analysis size: 512px width is enough for sharpness check
        const aWidth = 512;

        for (let i = 0; i < frameCount; i++) {
            if (captureId !== activeCaptureId) return null;
            // Capture raw frame
            const frameCanvas = document.createElement("canvas");
            frameCanvas.width = video.videoWidth;
            frameCanvas.height = video.videoHeight;
            frameCanvas.getContext("2d").drawImage(video, 0, 0);

            // Calculate sharpness score
            let score = 0;
            try {
                // Downscale for analysis
                const ar = video.videoHeight / video.videoWidth;
                analysisCanvas.width = aWidth;
                analysisCanvas.height = aWidth * ar;
                ctx.drawImage(
                    frameCanvas,
                    0,
                    0,
                    analysisCanvas.width,
                    analysisCanvas.height,
                );

                score = calculateSharpness(analysisCanvas);
            } catch (e) {
                console.warn("Sharpness calc failed", e);
            }

            console.log(`Frame ${i + 1} Score: ${score.toFixed(2)}`);

            if (score > bestScore) {
                bestScore = score;
                // Convert best frame to Image object immediately to hold it
                bestImg = await new Promise((resolve) => {
                    const img = new Image();
                    img.onload = () => resolve(img);
                    img.src = frameCanvas.toDataURL("image/jpeg");
                });
            }

            // Small delay between frames
            await wait(100);
        }

        console.log(`Selected best frame with score: ${bestScore.toFixed(2)}`);
        return bestImg;
    }

    function calculateSharpness(canvasSource) {
        if (!cvLoaded || !cv.Mat) return 0;

        let src = cv.imread(canvasSource);
        let gray = new cv.Mat();
        let laplacian = new cv.Mat();
        let score = 0;

        try {
            cv.cvtColor(src, gray, cv.COLOR_RGBA2GRAY, 0);
            cv.Laplacian(gray, laplacian, cv.CV_64F);

            let meanStd = new cv.Mat();
            let meanStdDev = new cv.Mat();
            cv.meanStdDev(laplacian, meanStd, meanStdDev);

            // Variance = stddev^2. Higher variance = more edges = sharper.
            let stddev = meanStdDev.doubleAt(0, 0);
            score = stddev * stddev;

            meanStd.delete();
            meanStdDev.delete();
        } catch (e) {
            console.warn("CV Error", e);
        } finally {
            src.delete();
            gray.delete();
            laplacian.delete();
        }
        return score;
    }

    // --- Cropping Logic ---

    function startCrop(captureId = activeCaptureId) {
        isCropping = true;
        isReviewing = false; // Not in filter review yet
        // Wait for DOM to render the crop image container
        setTimeout(() => {
            if (captureId !== activeCaptureId || !isCropping) return;
            const imageElement = document.getElementById("crop-image");
            if (imageElement && window.Cropper) {
                if (cropper) cropper.destroy();
                cropper = new Cropper(imageElement, {
                    viewMode: 1,
                    dragMode: "move",
                    autoCropArea: 0.8,
                    restore: false,
                    guides: true,
                    center: true,
                    highlight: false,
                    cropBoxMovable: true,
                    cropBoxResizable: true,
                    toggleDragModeOnDblclick: false,
                });
            }
        }, 100);
    }

    function applyCrop() {
        if (!cropper) return;
        const captureId = activeCaptureId;
        const targetField = activeCaptureField;
        const canvas = cropper.getCroppedCanvas();
        if (canvas) {
            // Update capturedImage to the cropped version
            const img = new Image();
            img.onload = () => {
                if (
                    captureId !== activeCaptureId ||
                    targetField !== activeCaptureField
                ) {
                    return;
                }
                capturedImage = img;
                isCropping = false;
                cropper.destroy();
                cropper = null;
                isReviewing = true; // Go to filter review
                processImage(false, captureId, targetField); // Process but don't auto-upload
            };
            img.src = canvas.toDataURL("image/jpeg");
        }
    }

    function cancelCrop() {
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }
        clearCaptureBuffer();
    }

    // --- OCR Processing Logic ---

    function processImage(
        autoUpload = false,
        captureId = activeCaptureId,
        targetField = activeCaptureField,
    ) {
        if (captureId !== activeCaptureId || targetField !== activeCaptureField) {
            return;
        }
        if (!capturedImage) return;
        isProcessing = true;
        const sourceImage = capturedImage;

        // Use requestAnimationFrame to prevent UI freeze
        requestAnimationFrame(() => {
            if (
                captureId !== activeCaptureId ||
                targetField !== activeCaptureField
            ) {
                return;
            }
            try {
                // 1. Prepare Source
                const tempCanvas = document.createElement("canvas");
                tempCanvas.width = sourceImage.width;
                tempCanvas.height = sourceImage.height;
                const tempCtx = tempCanvas.getContext("2d");
                tempCtx.drawImage(sourceImage, 0, 0);

                let srcCanvas = tempCanvas;

                // 2. Auto Crop (DISABLED/REMOVED in favor of manual crop)
                /* if (filters.autoCrop && scanner) { ... } */

                // 3. OpenCV Processing
                if (cvLoaded && cv.Mat) {
                    executeOpenCV(srcCanvas, autoUpload, captureId, targetField);
                } else {
                    // Fallback just draw canvas
                    const ctx = resultCanvas.getContext("2d");
                    resultCanvas.width = srcCanvas.width;
                    resultCanvas.height = srcCanvas.height;
                    ctx.drawImage(srcCanvas, 0, 0);

                    // Fallback simple upload for non-OpenCV path
                    resultCanvas.toBlob(
                        (blob) => {
                            if (
                                captureId !== activeCaptureId ||
                                targetField !== activeCaptureField
                            ) {
                                return;
                            }
                            if (!blob) {
                                error = "Failed to process image. Please retake.";
                                if (autoUpload) {
                                    clearCaptureBuffer();
                                }
                                isProcessing = false;
                                return;
                            }
                            processedBlob = blob;
                            processedBlobCaptureId = captureId;
                            processedBlobField = targetField;
                            isProcessing = false;
                            if (autoUpload) {
                                uploadProcessed(captureId, targetField);
                            }
                        },
                        "image/jpeg",
                        0.85,
                    );
                }
            } catch (e) {
                console.error("Processing failed", e);
                isProcessing = false;
                error = "Image processing failed. Try again/Turn off filters.";
            }
        });
    }

    function executeOpenCV(
        sourceCanvas,
        autoUpload = false,
        captureId = activeCaptureId,
        targetField = activeCaptureField,
    ) {
        let src = cv.imread(sourceCanvas);
        let dst = new cv.Mat();

        try {
            // Brightness & Contrast
            // dst = alpha * src + beta
            const alpha = parseFloat(filters.contrast);
            const beta = parseFloat(filters.brightness);
            src.convertTo(src, -1, alpha, beta);

            if (filters.type === "original") {
                cv.imshow(resultCanvas, src);
            } else {
                // Grayscale
                cv.cvtColor(src, src, cv.COLOR_RGBA2GRAY, 0);

                if (filters.type === "bw") {
                    // Denoise
                    const denoiseLevel = parseInt(filters.denoise);
                    if (denoiseLevel > 0) {
                        const kSize = denoiseLevel * 2 + 1;
                        cv.GaussianBlur(
                            src,
                            src,
                            new cv.Size(kSize, kSize),
                            0,
                            0,
                            cv.BORDER_DEFAULT,
                        );
                    }

                    // Adaptive Threshold
                    const blockSize = parseInt(filters.blockSize);
                    const validBlockSize =
                        blockSize % 2 === 0 ? blockSize + 1 : blockSize; // Must be odd
                    const thresholdC = parseInt(filters.threshold);

                    cv.adaptiveThreshold(
                        src,
                        dst,
                        255,
                        cv.ADAPTIVE_THRESH_GAUSSIAN_C,
                        cv.THRESH_BINARY,
                        validBlockSize,
                        thresholdC,
                    );
                    cv.imshow(resultCanvas, dst);
                } else {
                    // Gray only
                    cv.imshow(resultCanvas, src);
                }
            }

            // Update the blob for uploading
            resultCanvas.toBlob(
                (blob) => {
                    if (
                        captureId !== activeCaptureId ||
                        targetField !== activeCaptureField
                    ) {
                        return;
                    }
                    if (!blob) {
                        error = "Failed to process image. Please retake.";
                        if (autoUpload) {
                            clearCaptureBuffer();
                        }
                        isProcessing = false;
                        return;
                    }
                    processedBlob = blob;
                    processedBlobCaptureId = captureId;
                    processedBlobField = targetField;
                    isProcessing = false;
                    if (autoUpload) {
                        uploadProcessed(captureId, targetField);
                    }
                },
                "image/jpeg",
                0.85,
            );
        } catch (err) {
            console.error("OpenCV error", err);
            isProcessing = false;
        } finally {
            src.delete();
            dst.delete();
        }
    }

    function uploadProcessed(
        expectedCaptureId = activeCaptureId,
        expectedField = activeCaptureField,
    ) {
        if (isProcessing) return;
        if (!processedBlob) return;
        if (expectedCaptureId !== activeCaptureId) return;
        if (expectedField !== activeCaptureField) return;
        if (processedBlobCaptureId !== expectedCaptureId) return;
        if (processedBlobField !== expectedField) return;

        if (nextCaptureField !== expectedField) {
            error = "Capture sequence changed. Please retake the current page.";
            clearCaptureBuffer();
            return;
        }

        if (expectedField === "patientInfoImage") {
            queuedPatientInfoBlob = processedBlob;
            nextCaptureField = "operationImage";
            lastUploadStatus = "First image saved. Capture operation image.";
            error = null;
            clearCaptureBuffer();
            return;
        }

        if (!queuedPatientInfoBlob) {
            nextCaptureField = "patientInfoImage";
            error = "First image missing. Capture the first page again.";
            clearCaptureBuffer();
            return;
        }

        queuedOperationBlob = processedBlob;
        const patientBlob = queuedPatientInfoBlob;
        const operationBlob = queuedOperationBlob;
        const metadataSnapshot = JSON.parse(JSON.stringify(metadataPayload));

        resetUploadSequence();
        clearCaptureBuffer();
        error = null;
        queueUploadJob(patientBlob, operationBlob, metadataSnapshot);
        clearMetadataDraft();
        showMetadataPanel = false;
    }

    function cancelReview() {
        clearCaptureBuffer();
    }

    // Debounced updater for sliders
    let debounceTimer;
    function updateParams(key, value) {
        filters[key] = value;
        saveSettings(); // Save whenever changed
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            processImage();
        }, 100); // 100ms debounce
    }

    function setFilterType(type) {
        filters.type = type;
        saveSettings(); // Save whenever changed
        processImage();
    }

    function toggleReviewMode() {
        saveSettings(); // Save checking status
    }

    async function downloadExcel() {
        try {
            const response = await fetch(`${API_BASE_URL}/download`, {
                method: "GET",
                headers: {
                    "X-Password": API_PASSWORD,
                },
            });

            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = "patient_data.xlsx";
                document.body.appendChild(a);
                a.click();
                a.remove();
                window.URL.revokeObjectURL(url);
            } else if (response.status === 404) {
                alert("Excel file not found. Have you uploaded any images yet?");
            } else {
                alert("Download failed: " + response.status);
            }
        } catch (error) {
            console.error("Error downloading excel:", error);
            alert("Download failed.");
        }
    }

    onMount(() => {
        loadSettings();
        loadMetadataDraft();
        startCamera();
        initLibraries();
    });

    onDestroy(() => {
        if (cropper) cropper.destroy();
        if (stream) {
            stream.getTracks().forEach((track) => track.stop());
        }
    });
</script>

<div class="camera-app">
    {#if showMetadataPanel}
        <!-- svelte-ignore a11y_no_static_element_interactions a11y_click_events_have_key_events -->
        <div class="metadata-overlay" on:click={closeMetadataPanel} role="presentation">
            <!-- svelte-ignore a11y_no_static_element_interactions a11y_click_events_have_key_events -->
            <div
                class="metadata-sheet"
                role="dialog"
                aria-modal="true"
                aria-labelledby="metadata-title"
                on:click|stopPropagation
            >
                <div class="metadata-head">
                    <h3 id="metadata-title">Upload Metadata</h3>
                    <span>{metadataFieldCount} fields active</span>
                </div>

                <div class="metadata-fields">
                    <label class="metadata-label" for="metadata-operateur"
                        >Operateur</label
                    >
                    <select
                        id="metadata-operateur"
                        class="metadata-input"
                        value={metadataOperateur}
                        on:change={(e) =>
                            setMetadataField("operateur", e.target.value)}
                    >
                        <option value="">Nicht gesetzt</option>
                        {#each OPERATEUR_OPTIONS as option}
                            <option value={option}>{option}</option>
                        {/each}
                    </select>

                    <label class="metadata-label" for="metadata-patient"
                        >Patient Name</label
                    >
                    <input
                        id="metadata-patient"
                        class="metadata-input"
                        type="text"
                        autocomplete="off"
                        placeholder="Erika Mustermann"
                        value={metadataNamePatient}
                        on:input={(e) =>
                            setMetadataField("name_patient", e.target.value)}
                    />

                    <label class="metadata-label" for="metadata-birthdate"
                        >Geburtsdatum</label
                    >
                    <input
                        id="metadata-birthdate"
                        class="metadata-input"
                        type="date"
                        autocomplete="off"
                        value={metadataGeburtsdatum}
                        on:input={(e) =>
                            setMetadataField("geburtsdatum", e.target.value)}
                    />
                </div>

                <div class="metadata-extra-header">
                    <h4>Schema Fields</h4>
                    <button
                        class="btn secondary"
                        on:click={addKnownMetadataField}>Add</button
                    >
                </div>

                {#if metadataKnownFields.length === 0}
                    <p class="metadata-empty">
                        No schema fields selected yet.
                    </p>
                {/if}

                <div class="metadata-extra-list">
                    {#each metadataKnownFields as pair, index (index)}
                        <div class="metadata-extra-row">
                            <select
                                class="metadata-input"
                                value={pair.key}
                                on:change={(e) =>
                                    updateKnownMetadataField(
                                        index,
                                        "key",
                                        e.target.value,
                                    )}
                            >
                                <option value="">Field auswählen...</option>
                                {#each METADATA_FIELD_GROUPS as group}
                                    <optgroup label={group.section}>
                                        {#each group.fields as field}
                                            <option value={field.key}
                                                >{field.label}</option
                                            >
                                        {/each}
                                    </optgroup>
                                {/each}
                            </select>

                            {#if getMetadataFieldDefinition(pair.key)?.options}
                                <select
                                    class="metadata-input"
                                    value={pair.value}
                                    on:change={(e) =>
                                        updateKnownMetadataField(
                                            index,
                                            "value",
                                            e.target.value,
                                        )}
                                >
                                    <option value="">Wert auswählen...</option>
                                    {#each getMetadataFieldDefinition(pair.key)
                                        ?.options ?? [] as option}
                                        <option value={option}>{option}</option>
                                    {/each}
                                </select>
                            {:else}
                                <input
                                    class="metadata-input"
                                    type={getMetadataInputType(pair.key)}
                                    step={getMetadataInputStep(pair.key)}
                                    inputmode={getMetadataInputMode(pair.key)}
                                    autocomplete="off"
                                    placeholder="Wert"
                                    value={pair.value}
                                    on:input={(e) =>
                                        updateKnownMetadataField(
                                            index,
                                            "value",
                                            e.target.value,
                                        )}
                                />
                            {/if}

                            <button
                                class="btn secondary metadata-remove"
                                on:click={() => removeKnownMetadataField(index)}
                                aria-label="Remove schema metadata field"
                                >Remove</button
                            >
                        </div>
                    {/each}
                </div>

                <div class="metadata-extra-header">
                    <h4>Custom Fields</h4>
                    <button class="btn secondary" on:click={addMetadataExtra}
                        >Add</button
                    >
                </div>

                {#if metadataExtras.length === 0}
                    <p class="metadata-empty">
                        No custom fields yet. Add only when needed.
                    </p>
                {/if}

                <div class="metadata-extra-list">
                    {#each metadataExtras as pair, index (index)}
                        <div class="metadata-extra-row">
                            <input
                                class="metadata-input"
                                type="text"
                                autocomplete="off"
                                placeholder="field_key"
                                value={pair.key}
                                on:input={(e) =>
                                    updateMetadataExtra(
                                        index,
                                        "key",
                                        e.target.value,
                                    )}
                            />
                            <input
                                class="metadata-input"
                                type="text"
                                autocomplete="off"
                                placeholder="value"
                                value={pair.value}
                                on:input={(e) =>
                                    updateMetadataExtra(
                                        index,
                                        "value",
                                        e.target.value,
                                    )}
                            />
                            <button
                                class="btn secondary metadata-remove"
                                on:click={() => removeMetadataExtra(index)}
                                aria-label="Remove custom metadata field"
                                >Remove</button
                            >
                        </div>
                    {/each}
                </div>

                <p class="metadata-note">
                    Metadata is optional. Provided values override OCR output.
                    Unknown keys are ignored by the API.
                </p>

                <div class="metadata-actions">
                    <button class="btn secondary" on:click={clearMetadataDraft}
                        >Clear</button
                    >
                    <button class="btn primary" on:click={closeMetadataPanel}
                        >Done</button
                    >
                </div>
            </div>
        </div>
    {/if}

    {#if error}
        <div class="error-message">
            <p>{error}</p>
            <div class="error-actions">
                <button on:click={() => (error = null)} class="btn secondary"
                    >Dismiss</button
                >
                <button on:click={startCamera} class="btn primary"
                    >Restart Camera</button
                >
            </div>
        </div>
    {/if}

    <div
        class="viewfinder"
        style:display={isReviewing || isCropping ? "none" : "flex"}
    >
        <!-- svelte-ignore a11y-media-has-caption -->
        <video bind:this={video} playsinline muted></video>
        <canvas bind:this={canvas} style="display: none;"></canvas>

        {#if isFlashing}
            <div class="flash-overlay"></div>
        {/if}

        {#if pendingUploads > 0 || uploadQueueSize > 0 || isUploadWorkerRunning || lastUploadStatus || (isProcessing && !isReviewing)}
            <div class="status-indicator">
                {#if pendingUploads > 0}
                    <span class="spinner"></span>
                    Uploading... {pendingUploads + uploadQueueSize} pending
                {:else if isUploadWorkerRunning || uploadQueueSize > 0}
                    <span class="spinner"></span>
                    Upload queue active... {uploadQueueSize +
                        (isUploadWorkerRunning ? 1 : 0)} pending
                {:else if isProcessing && !isReviewing}
                    <span class="spinner"></span>
                    Processing...
                {:else}
                    {lastUploadStatus}
                {/if}
            </div>
        {/if}

        <!-- Top Bar -->
        <div class="top-bar">
            <div class="top-bar-left">
                <button
                    class="meta-btn"
                    on:click={openMetadataPanel}
                    title="Edit Metadata"
                    aria-label="Edit Metadata"
                >
                    Meta
                    {#if metadataFieldCount > 0}
                        <span class="meta-count">{metadataFieldCount}</span>
                    {/if}
                </button>

                <!-- Download -->
                <button
                    class="icon-btn-small"
                    on:click={downloadExcel}
                    title="Download Excel"
                    aria-label="Download Excel"
                >
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="20"
                        height="20"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="2"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        ><path
                            d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"
                        /><polyline points="7 10 12 15 17 10" /><line
                            x1="12"
                            x2="12"
                            y1="15"
                            y2="3"
                        /></svg
                    >
                </button>
            </div>

            <!-- Right Side: Toggle -->
            <div class="toggle-container">
                <label class="switch-label" for="review-mode-toggle">
                    <span>Review Mode</span>
                    <input
                        id="review-mode-toggle"
                        type="checkbox"
                        bind:checked={reviewModeEnabled}
                        on:change={toggleReviewMode}
                    />
                    <div class="switch"></div>
                </label>
            </div>
            {#if !cvLoaded}
                <span class="loading-badge">Loading AI...</span>
            {/if}
        </div>

        <div class="capture-step-indicator">
            <strong>{getCaptureStepLabel()}</strong>
            <span>{getCaptureStepHint()}</span>
            {#if metadataFieldCount > 0}
                <span class="capture-meta-note"
                    >Metadata ready: {metadataFieldCount} fields</span
                >
            {/if}
        </div>

        <div class="controls">
            <button
                on:click={switchCamera}
                class="btn icon-btn"
                title="Switch Camera"
                aria-label="Switch Camera"
            >
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="24"
                    height="24"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    ><path
                        d="M20 5H9l-7 7 7 7h11a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2Z"
                    /><line x1="18" y1="9" x2="12" y2="15" /><line
                        x1="12"
                        y1="9"
                        x2="18"
                        y2="15"
                    /></svg
                >
            </button>

            {#if torchSupported}
                <button
                    on:click={toggleTorch}
                    class="btn icon-btn"
                    class:active={torchEnabled}
                    title={torchEnabled ? "Turn off flash" : "Turn on flash"}
                >
                    {#if torchEnabled}
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="24"
                            height="24"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            ><path
                                d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"
                            /><path d="M9 18h6" /><path d="M10 22h4" /></svg
                        >
                    {:else}
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="24"
                            height="24"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            ><path
                                d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"
                            /><path d="M9 18h6" /><path d="M10 22h4" /><line
                                x1="4"
                                x2="20"
                                y1="2"
                                y2="22"
                            /></svg
                        >
                    {/if}
                </button>
            {:else}
                <div class="spacer"></div>
            {/if}

            <button
                on:click={capturePhoto}
                class="capture-btn"
                disabled={captureDisabled}
                aria-label={nextCaptureField === "patientInfoImage"
                    ? "Take patient info photo"
                    : "Take operation photo"}
            ></button>

            {#if !torchSupported}
                <div class="spacer"></div>
            {/if}
        </div>
    </div>

    <!-- Crop View -->
    <div class="crop-view" style:display={isCropping ? "flex" : "none"}>
        <div class="crop-container">
            {#if originalImage}
                <img
                    id="crop-image"
                    src={originalImage.src}
                    alt="Crop target"
                    style="max-width: 100%; display: block;"
                />
            {/if}
        </div>
        <div class="crop-controls">
            <button class="btn secondary" on:click={cancelCrop}>Cancel</button>
            <button class="btn primary" on:click={applyCrop}>Done</button>
        </div>
    </div>

    <!-- Review / Editor View -->
    <div class="editor" style:display={isReviewing ? "flex" : "none"}>
        <div class="editor-canvas-container">
            <canvas bind:this={resultCanvas}></canvas>
            {#if isProcessing}
                <div class="processing-overlay">
                    <span class="spinner"></span>
                </div>
            {/if}
        </div>

        <div class="editor-controls">
            <!-- Filter Tabs -->
            <div class="filter-tabs">
                <button
                    class:active={filters.type === "original"}
                    on:click={() => setFilterType("original")}>Original</button
                >
                <button
                    class:active={filters.type === "gray"}
                    on:click={() => setFilterType("gray")}>Gray</button
                >
                <button
                    class:active={filters.type === "bw"}
                    on:click={() => setFilterType("bw")}>B&W (OCR)</button
                >
            </div>

            <!-- Sliders -->
            <div class="settings-panel">
                <div class="setting-row">
                    <label for="brightness-slider">Brightness</label>
                    <input
                        id="brightness-slider"
                        type="range"
                        min="-100"
                        max="100"
                        value={filters.brightness}
                        on:input={(e) =>
                            updateParams("brightness", e.target.value)}
                    />
                </div>
                <div class="setting-row">
                    <label for="contrast-slider">Contrast</label>
                    <input
                        id="contrast-slider"
                        type="range"
                        min="0.1"
                        max="3.0"
                        step="0.1"
                        value={filters.contrast}
                        on:input={(e) =>
                            updateParams("contrast", e.target.value)}
                    />
                </div>

                {#if filters.type === "bw"}
                    <div class="setting-row">
                        <label for="threshold-slider">Threshold</label>
                        <input
                            id="threshold-slider"
                            type="range"
                            min="0"
                            max="50"
                            value={filters.threshold}
                            on:input={(e) =>
                                updateParams("threshold", e.target.value)}
                        />
                    </div>
                    <div class="setting-row">
                        <label for="blocksize-slider">Block Size</label>
                        <input
                            id="blocksize-slider"
                            type="range"
                            min="3"
                            max="151"
                            step="2"
                            value={filters.blockSize}
                            on:input={(e) =>
                                updateParams("blockSize", e.target.value)}
                        />
                    </div>
                {/if}

                <!-- Remove Auto-Crop toggle from UI since we have manual crop now -->
            </div>

            <!-- Action Buttons -->
            <div class="action-buttons">
                <button class="btn secondary" on:click={cancelReview}
                    >Retake</button
                >
                <button
                    class="btn primary"
                    on:click={uploadProcessed}
                    disabled={saveDisabled}
                >
                    >{nextCaptureField === "patientInfoImage"
                        ? "Save First Image"
                        : "Upload Both Images"}</button
                >
            </div>
        </div>
    </div>
</div>

<style>
    .camera-app {
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        background-color: #000;
        color: white;
        display: flex;
        flex-direction: column;
        z-index: 9999; /* Ensure it stays on top */
        font-family:
            system-ui,
            -apple-system,
            sans-serif;
    }

    .error-message {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100%;
        padding: 2rem;
        text-align: center;
        z-index: 20;
        background: #000;
    }

    .error-actions {
        display: flex;
        gap: 1rem;
        margin-top: 1.5rem;
    }

    .viewfinder {
        position: relative;
        width: 100%;
        height: 100%;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        overflow: hidden;
    }

    video {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }

    .flash-overlay {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: white;
        z-index: 10;
        pointer-events: none;
        opacity: 0;
        animation: flash 150ms ease-out;
    }

    @keyframes flash {
        0% {
            opacity: 0.8;
        }
        100% {
            opacity: 0;
        }
    }

    .status-indicator {
        position: absolute;
        top: env(safe-area-inset-top, 50px);
        margin-top: 1rem;
        left: 50%;
        transform: translateX(-50%);
        background: rgba(0, 0, 0, 0.6);
        color: white;
        padding: 0.5rem 1rem;
        border-radius: 20px;
        z-index: 25;
        font-size: 0.9rem;
        display: flex;
        align-items: center;
        gap: 0.6rem;
        backdrop-filter: blur(4px);
        -webkit-backdrop-filter: blur(4px);
    }

    /* Top Bar for Review Mode Toggle */
    .top-bar {
        position: absolute;
        top: env(safe-area-inset-top, 10px);
        left: 0;
        width: 100%;
        padding: 1rem;
        display: flex;
        justify-content: space-between;
        align-items: center;
        z-index: 20;
    }

    .top-bar-left {
        display: flex;
        align-items: center;
        gap: 0.6rem;
    }

    .meta-btn {
        border: none;
        color: white;
        background: rgba(0, 0, 0, 0.45);
        border-radius: 999px;
        padding: 0.45rem 0.75rem;
        font-size: 0.85rem;
        font-weight: 600;
        display: flex;
        align-items: center;
        gap: 0.45rem;
        backdrop-filter: blur(5px);
        -webkit-backdrop-filter: blur(5px);
    }

    .meta-count {
        min-width: 1.25rem;
        height: 1.25rem;
        border-radius: 999px;
        background: #22c55e;
        color: #052e16;
        font-size: 0.72rem;
        font-weight: 700;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0 0.25rem;
    }

    .capture-step-indicator {
        position: absolute;
        top: calc(env(safe-area-inset-top, 10px) + 72px);
        left: 50%;
        transform: translateX(-50%);
        background: rgba(0, 0, 0, 0.6);
        border: 1px solid rgba(255, 255, 255, 0.16);
        border-radius: 12px;
        padding: 0.55rem 0.8rem;
        display: flex;
        flex-direction: column;
        gap: 0.2rem;
        text-align: center;
        max-width: min(92vw, 420px);
        z-index: 20;
        backdrop-filter: blur(6px);
        -webkit-backdrop-filter: blur(6px);
    }

    .capture-step-indicator strong {
        font-size: 0.88rem;
        font-weight: 700;
    }

    .capture-step-indicator span {
        font-size: 0.76rem;
        color: #d4d4d8;
    }

    .capture-meta-note {
        color: #86efac;
    }

    .toggle-container {
        background: rgba(0, 0, 0, 0.5);
        backdrop-filter: blur(10px);
        padding: 0.5rem 1rem;
        border-radius: 20px;
    }

    .switch-label {
        display: flex;
        align-items: center;
        gap: 10px;
        font-size: 0.9rem;
        cursor: pointer;
    }

    .switch-label input {
        display: none;
    }

    .switch {
        width: 40px;
        height: 22px;
        background: #555;
        border-radius: 20px;
        position: relative;
        transition: background 0.3s;
    }

    .switch::after {
        content: "";
        position: absolute;
        top: 2px;
        left: 2px;
        width: 18px;
        height: 18px;
        background: white;
        border-radius: 50%;
        transition: transform 0.3s;
    }

    .switch-label input:checked + .switch {
        background: #4ade80; /* green */
    }

    .switch-label input:checked + .switch::after {
        transform: translateX(18px);
    }

    .icon-btn-small {
        background: rgba(0, 0, 0, 0.4);
        border: none;
        color: white;
        padding: 8px;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        backdrop-filter: blur(5px);
    }

    .loading-badge {
        font-size: 0.8rem;
        background: rgba(234, 179, 8, 0.8);
        color: black;
        padding: 0.2rem 0.6rem;
        border-radius: 10px;
        font-weight: 600;
    }

    .spinner {
        width: 12px;
        height: 12px;
        border: 2px solid rgba(255, 255, 255, 0.3);
        border-top-color: white;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }

    @keyframes spin {
        to {
            transform: rotate(360deg);
        }
    }

    .controls {
        position: absolute;
        bottom: 2rem;
        left: 50%;
        transform: translateX(-50%);
        width: calc(100% - 3rem);
        max-width: 400px;
        padding: 1.25rem 1.5rem;
        background: rgba(0, 0, 0, 0.5);
        backdrop-filter: blur(20px);
        -webkit-backdrop-filter: blur(20px);
        border-radius: 2rem;
        display: flex;
        justify-content: space-around;
        align-items: center;
        gap: 1rem;
        margin-bottom: env(safe-area-inset-bottom, 0px);
        z-index: 5;
    }

    .btn {
        background: rgba(255, 255, 255, 0.2);
        border: none;
        color: white;
        padding: 0.8rem 1.5rem;
        border-radius: 2rem;
        font-weight: 600;
        backdrop-filter: blur(10px);
        cursor: pointer;
        font-size: 1rem;
        transition: background 0.2s;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .btn.active {
        background: rgba(255, 255, 255, 0.8);
        color: black;
    }

    .btn:hover {
        background: rgba(255, 255, 255, 0.3);
    }

    .btn:disabled {
        opacity: 0.55;
        cursor: not-allowed;
        pointer-events: none;
    }

    .btn.primary {
        background: white;
        color: black;
    }

    .btn.secondary {
        background: rgba(255, 255, 255, 0.2);
    }

    .btn.icon-btn {
        padding: 0.8rem;
        border-radius: 50%;
    }

    .capture-btn {
        width: 72px;
        height: 72px;
        border-radius: 50%;
        border: 4px solid white;
        background: transparent;
        position: relative;
        cursor: pointer;
        padding: 0;
    }

    .capture-btn:disabled {
        opacity: 0.55;
        cursor: not-allowed;
    }

    .capture-btn::after {
        content: "";
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        width: 60px;
        height: 60px;
        background: white;
        border-radius: 50%;
        transition: transform 0.1s;
    }

    .capture-btn:active::after {
        transform: translate(-50%, -50%) scale(0.9);
        background: #eee;
    }

    .spacer {
        width: 40px; /* Balance the layout for icon button */
    }

    /* --- Editor Styles --- */
    .editor {
        width: 100%;
        height: 100%;
        display: flex;
        flex-direction: column;
        background: #111;
        z-index: 30;
    }

    .editor-canvas-container {
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
        overflow: hidden;
        position: relative;
        padding: 20px;
    }

    .editor-canvas-container canvas {
        max-width: 100%;
        max-height: 100%;
        object-fit: contain;
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.5);
    }

    .processing-overlay {
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .editor-controls {
        background: #222;
        padding: 1.5rem;
        border-top-left-radius: 20px;
        border-top-right-radius: 20px;
        padding-bottom: max(1.5rem, env(safe-area-inset-bottom));
    }

    .filter-tabs {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 1.5rem;
        background: #333;
        padding: 4px;
        border-radius: 12px;
    }

    .filter-tabs button {
        flex: 1;
        background: transparent;
        border: none;
        color: #888;
        padding: 8px;
        border-radius: 8px;
        font-size: 0.9rem;
        font-weight: 500;
        cursor: pointer;
    }

    .filter-tabs button.active {
        background: #555;
        color: white;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
    }

    .settings-panel {
        display: flex;
        flex-direction: column;
        gap: 1rem;
        margin-bottom: 1.5rem;
    }

    .setting-row {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
    }

    .setting-row label {
        color: #aaa;
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.5px;
    }

    .setting-row input[type="range"] {
        width: 100%;
        height: 4px;
        background: #444;
        border-radius: 2px;
        appearance: none;
    }

    .setting-row input[type="range"]::-webkit-slider-thumb {
        appearance: none;
        width: 20px;
        height: 20px;
        background: white;
        border-radius: 50%;
        cursor: pointer;
    }

    .action-buttons {
        display: flex;
        gap: 1rem;
    }

    .action-buttons button {
        flex: 1;
    }

    /* --- Crop View Styles --- */
    .crop-view {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: black;
        z-index: 40;
        flex-direction: column;
    }

    .crop-container {
        flex: 1;
        overflow: hidden;
        position: relative;
        background: #111;
    }

    .crop-controls {
        height: 80px;
        background: #222;
        display: flex;
        align-items: center;
        justify-content: space-around;
        gap: 1rem;
        padding: 0 1rem;
        padding-bottom: env(safe-area-inset-bottom);
    }

    .metadata-overlay {
        position: fixed;
        inset: 0;
        z-index: 80;
        background: rgba(0, 0, 0, 0.55);
        display: flex;
        align-items: flex-end;
        justify-content: center;
    }

    .metadata-sheet {
        width: min(100%, 560px);
        max-height: 84vh;
        overflow-y: auto;
        background: #171717;
        border-top-left-radius: 18px;
        border-top-right-radius: 18px;
        border: 1px solid rgba(255, 255, 255, 0.12);
        border-bottom: none;
        padding: 1rem;
        padding-bottom: max(1rem, env(safe-area-inset-bottom));
    }

    .metadata-head {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        margin-bottom: 0.8rem;
    }

    .metadata-head h3 {
        margin: 0;
        font-size: 1rem;
    }

    .metadata-head span {
        color: #a3a3a3;
        font-size: 0.8rem;
    }

    .metadata-fields {
        display: flex;
        flex-direction: column;
        gap: 0.45rem;
    }

    .metadata-label {
        color: #cbd5e1;
        font-size: 0.76rem;
        margin-top: 0.2rem;
    }

    .metadata-input {
        width: 100%;
        box-sizing: border-box;
        background: #262626;
        border: 1px solid #3f3f46;
        color: white;
        border-radius: 10px;
        padding: 0.7rem 0.75rem;
        font-size: 0.92rem;
    }

    .metadata-input:focus {
        outline: none;
        border-color: #60a5fa;
    }

    .metadata-extra-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-top: 1rem;
        margin-bottom: 0.5rem;
    }

    .metadata-extra-header h4 {
        margin: 0;
        font-size: 0.88rem;
    }

    .metadata-extra-list {
        display: flex;
        flex-direction: column;
        gap: 0.6rem;
    }

    .metadata-extra-row {
        display: grid;
        grid-template-columns: 1fr;
        gap: 0.45rem;
    }

    .metadata-remove {
        width: 100%;
    }

    .metadata-empty {
        margin: 0;
        color: #a3a3a3;
        font-size: 0.8rem;
    }

    .metadata-note {
        margin: 0.9rem 0 0;
        color: #a1a1aa;
        font-size: 0.8rem;
    }

    .metadata-actions {
        margin-top: 0.9rem;
        display: flex;
        gap: 0.75rem;
        justify-content: flex-end;
    }

    @media (min-width: 720px) {
        .metadata-overlay {
            align-items: center;
            padding: 1rem;
        }

        .metadata-sheet {
            border-radius: 16px;
            border-bottom: 1px solid rgba(255, 255, 255, 0.12);
            max-height: 88vh;
        }

        .metadata-extra-row {
            grid-template-columns: 1fr 1fr auto;
            align-items: center;
        }

        .metadata-remove {
            width: auto;
        }
    }

</style>
