const DATABASE_NAME = "camera-app-uploads";
const DATABASE_VERSION = 1;
const ACTIVE_DRAFT_ID = "active";
const METADATA_CLEAR_MARKER_ID = "metadata-clear-pending";

let databasePromise;

function requestResult(request) {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function transactionComplete(transaction) {
    return new Promise((resolve, reject) => {
        transaction.oncomplete = () => resolve();
        transaction.onabort = () =>
            reject(transaction.error ?? new Error("IndexedDB transaction aborted."));
        transaction.onerror = () =>
            reject(transaction.error ?? new Error("IndexedDB transaction failed."));
    });
}

export function openCameraUploadDatabase() {
    if (databasePromise) return databasePromise;
    if (typeof indexedDB === "undefined") {
        return Promise.reject(new Error("IndexedDB is not available."));
    }

    databasePromise = new Promise((resolve, reject) => {
        const request = indexedDB.open(DATABASE_NAME, DATABASE_VERSION);
        request.onupgradeneeded = () => {
            const database = request.result;
            if (!database.objectStoreNames.contains("drafts")) {
                database.createObjectStore("drafts", { keyPath: "id" });
            }
            if (!database.objectStoreNames.contains("jobs")) {
                const jobs = database.createObjectStore("jobs", {
                    keyPath: "id",
                });
                jobs.createIndex("nextAttemptAt", "nextAttemptAt");
                jobs.createIndex("createdAt", "createdAt");
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
        request.onblocked = () =>
            reject(new Error("Camera storage upgrade is blocked by another tab."));
    });

    return databasePromise;
}

export async function getActiveCaptureDraft() {
    const database = await openCameraUploadDatabase();
    const transaction = database.transaction("drafts", "readonly");
    const completed = transactionComplete(transaction);
    const result = await requestResult(
        transaction.objectStore("drafts").get(ACTIVE_DRAFT_ID),
    );
    await completed;
    return result ?? null;
}

export async function saveActiveCaptureDraft(patientInfoBlob, metadataDraft) {
    if (!(patientInfoBlob instanceof Blob) || patientInfoBlob.size === 0) {
        throw new Error("The first image is empty and cannot be saved.");
    }

    const database = await openCameraUploadDatabase();
    const transaction = database.transaction("drafts", "readwrite");
    const completed = transactionComplete(transaction);
    transaction.objectStore("drafts").put({
        id: ACTIVE_DRAFT_ID,
        patientInfoBlob,
        metadataDraft,
        updatedAt: Date.now(),
    });
    await completed;
}

export async function clearActiveCaptureDraft() {
    const database = await openCameraUploadDatabase();
    const transaction = database.transaction("drafts", "readwrite");
    const completed = transactionComplete(transaction);
    transaction.objectStore("drafts").delete(ACTIVE_DRAFT_ID);
    await completed;
}

export async function createUploadJobFromDraft(job) {
    if (!(job.operationBlob instanceof Blob) || job.operationBlob.size === 0) {
        throw new Error("The second image is empty and cannot be saved.");
    }

    const database = await openCameraUploadDatabase();
    const transaction = database.transaction(["drafts", "jobs"], "readwrite");
    const completed = transactionComplete(transaction);
    const draft = await requestResult(
        transaction.objectStore("drafts").get(ACTIVE_DRAFT_ID),
    );

    if (!(draft?.patientInfoBlob instanceof Blob) || draft.patientInfoBlob.size === 0) {
        await completed;
        throw new Error("The saved first image is missing.");
    }

    transaction.objectStore("jobs").add({
        id: job.id,
        patientInfoBlob: draft.patientInfoBlob,
        operationBlob: job.operationBlob,
        metadata: job.metadata,
        createdAt: job.createdAt,
        updatedAt: job.createdAt,
        status: "pending",
        attemptCount: 0,
        nextAttemptAt: job.createdAt,
        lastError: null,
    });
    const drafts = transaction.objectStore("drafts");
    drafts.delete(ACTIVE_DRAFT_ID);
    drafts.put({ id: METADATA_CLEAR_MARKER_ID, createdAt: job.createdAt });
    await completed;
}

export async function consumeMetadataClearMarker() {
    const database = await openCameraUploadDatabase();
    const transaction = database.transaction("drafts", "readwrite");
    const completed = transactionComplete(transaction);
    const store = transaction.objectStore("drafts");
    const marker = await requestResult(store.get(METADATA_CLEAR_MARKER_ID));
    if (marker) store.delete(METADATA_CLEAR_MARKER_ID);
    await completed;
    return Boolean(marker);
}

export async function listUploadJobs() {
    const database = await openCameraUploadDatabase();
    const transaction = database.transaction("jobs", "readonly");
    const completed = transactionComplete(transaction);
    const jobs = await requestResult(transaction.objectStore("jobs").getAll());
    await completed;
    return jobs.sort((left, right) => left.createdAt - right.createdAt);
}

export async function updateUploadJob(jobId, changes) {
    const database = await openCameraUploadDatabase();
    const transaction = database.transaction("jobs", "readwrite");
    const completed = transactionComplete(transaction);
    const store = transaction.objectStore("jobs");
    const current = await requestResult(store.get(jobId));
    if (!current) {
        await completed;
        return null;
    }

    const updated = { ...current, ...changes, id: current.id, updatedAt: Date.now() };
    store.put(updated);
    await completed;
    return updated;
}

export async function deleteUploadJob(jobId) {
    const database = await openCameraUploadDatabase();
    const transaction = database.transaction("jobs", "readwrite");
    const completed = transactionComplete(transaction);
    transaction.objectStore("jobs").delete(jobId);
    await completed;
}

export async function resetInterruptedUploads() {
    const jobs = await listUploadJobs();
    await Promise.all(
        jobs
            .filter((job) => job.status === "uploading")
            .map((job) =>
                updateUploadJob(job.id, {
                    status: "pending",
                    nextAttemptAt: Date.now(),
                    lastError: "Upload interrupted before acknowledgement.",
                }),
            ),
    );
}

export async function makeAllUploadsDueNow() {
    const jobs = await listUploadJobs();
    await Promise.all(
        jobs.map((job) =>
            updateUploadJob(job.id, {
                status: "pending",
                nextAttemptAt: Date.now(),
            }),
        ),
    );
}

export const cameraUploadStoreConstants = {
    databaseName: DATABASE_NAME,
    activeDraftId: ACTIVE_DRAFT_ID,
};
