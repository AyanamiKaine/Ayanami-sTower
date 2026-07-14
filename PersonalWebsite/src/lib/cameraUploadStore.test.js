import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it } from "vitest";
import {
    clearActiveCaptureDraft,
    consumeMetadataClearMarker,
    createUploadJobFromDraft,
    deleteUploadJob,
    getActiveCaptureDraft,
    listUploadJobs,
    makeAllUploadsDueNow,
    openCameraUploadDatabase,
    resetInterruptedUploads,
    saveActiveCaptureDraft,
    updateUploadJob,
} from "./cameraUploadStore.js";

async function clearDatabase() {
    const database = await openCameraUploadDatabase();
    const transaction = database.transaction(["drafts", "jobs"], "readwrite");
    transaction.objectStore("drafts").clear();
    transaction.objectStore("jobs").clear();
    await new Promise((resolve, reject) => {
        transaction.oncomplete = resolve;
        transaction.onerror = () => reject(transaction.error);
        transaction.onabort = () => reject(transaction.error);
    });
}

describe("camera upload storage", () => {
    beforeEach(clearDatabase);

    it("restores a saved first-page draft", async () => {
        const image = new Blob(["patient"], { type: "image/jpeg" });
        const metadataDraft = { name_patient: "Example" };

        await saveActiveCaptureDraft(image, metadataDraft);

        const restored = await getActiveCaptureDraft();
        expect(await restored.patientInfoBlob.text()).toBe("patient");
        expect(restored.metadataDraft).toEqual(metadataDraft);
    });

    it("atomically turns a draft into a complete pending job", async () => {
        await saveActiveCaptureDraft(
            new Blob(["patient"], { type: "image/jpeg" }),
            { name_patient: "Example" },
        );

        await createUploadJobFromDraft({
            id: "1234567890abcdef1234567890abcdef",
            operationBlob: new Blob(["operation"], { type: "image/jpeg" }),
            metadata: { operateur: "Example" },
            createdAt: 100,
        });

        expect(await getActiveCaptureDraft()).toBeNull();
        expect(await consumeMetadataClearMarker()).toBe(true);
        expect(await consumeMetadataClearMarker()).toBe(false);
        const jobs = await listUploadJobs();
        expect(jobs).toHaveLength(1);
        expect(await jobs[0].patientInfoBlob.text()).toBe("patient");
        expect(await jobs[0].operationBlob.text()).toBe("operation");
        expect(jobs[0]).toMatchObject({
            status: "pending",
            attemptCount: 0,
            nextAttemptAt: 100,
        });
    });

    it("does not create a job when the first page is missing", async () => {
        await expect(
            createUploadJobFromDraft({
                id: "1234567890abcdef1234567890abcdef",
                operationBlob: new Blob(["operation"]),
                metadata: null,
                createdAt: 100,
            }),
        ).rejects.toThrow("saved first image is missing");
        expect(await listUploadJobs()).toEqual([]);
    });

    it("recovers interrupted jobs and makes delayed jobs due", async () => {
        await saveActiveCaptureDraft(new Blob(["patient"]), null);
        await createUploadJobFromDraft({
            id: "1234567890abcdef1234567890abcdef",
            operationBlob: new Blob(["operation"]),
            metadata: null,
            createdAt: 100,
        });
        await updateUploadJob("1234567890abcdef1234567890abcdef", {
            status: "uploading",
            attemptCount: 2,
            nextAttemptAt: Date.now() + 60_000,
        });

        await resetInterruptedUploads();
        let [job] = await listUploadJobs();
        expect(job.status).toBe("pending");
        expect(job.lastError).toContain("interrupted");

        await updateUploadJob(job.id, { nextAttemptAt: Date.now() + 60_000 });
        await makeAllUploadsDueNow();
        [job] = await listUploadJobs();
        expect(job.nextAttemptAt).toBeLessThanOrEqual(Date.now());
    });

    it("only removes data through explicit deletion", async () => {
        await saveActiveCaptureDraft(new Blob(["patient"]), null);
        await clearActiveCaptureDraft();
        expect(await getActiveCaptureDraft()).toBeNull();

        await saveActiveCaptureDraft(new Blob(["patient"]), null);
        await createUploadJobFromDraft({
            id: "1234567890abcdef1234567890abcdef",
            operationBlob: new Blob(["operation"]),
            metadata: null,
            createdAt: 100,
        });
        await deleteUploadJob("1234567890abcdef1234567890abcdef");
        expect(await listUploadJobs()).toEqual([]);
    });
});
