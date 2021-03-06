// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _GCINTERFACE_EE_H_
#define _GCINTERFACE_EE_H_

// This interface provides the interface that the GC will use to speak to the rest
// of the execution engine. Everything that the GC does that requires the EE
// to be informed or that requires EE action must go through this interface.
//
// When BUILD_AS_STANDALONE is defined, this class is named IGCToCLR and is
// an abstract class. The EE will provide a class that fulfills this interface,
// and the GC will dispatch virtually on it to call into the EE. When BUILD_AS_STANDALONE
// is not defined, this class is named GCToEEInterface and the GC will dispatch statically on it.
class IGCToCLR {
public:
    // Suspends the EE for the given reason.
    virtual
    void SuspendEE(SUSPEND_REASON reason) = 0;

    // Resumes all paused threads, with a boolean indicating
    // if the EE is being restarted because a GC is complete.
    virtual
    void RestartEE(bool bFinishedGC) = 0;

    // Performs a stack walk of all managed threads and invokes the given promote_func
    // on all GC roots encountered on the stack. Depending on the condemned generation,
    // this function may also enumerate all static GC refs if necessary.
    virtual
    void GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc) = 0;

    // Callback from the GC informing the EE that it is preparing to start working.
    virtual
    void GcStartWork(int condemned, int max_gen) = 0;

    // Callback from the GC informing the EE that it has completed the managed stack
    // scan. User threads are still suspended at this point.
    virtual
    void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc) = 0;

    // Callback from the GC informing the EE that the background sweep phase of a BGC is
    // about to begin.
    virtual
    void GcBeforeBGCSweepWork() = 0;

    // Callback from the GC informing the EE that a GC has completed.
    virtual
    void GcDone(int condemned) = 0;

    // Predicate for the GC to query whether or not a given refcounted handle should
    // be promoted.
    virtual
    bool RefCountedHandleCallbacks(Object * pObject) = 0;

    // Performs a weak pointer scan of the sync block cache.
    virtual
    void SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2) = 0;

    // Indicates to the EE that the GC intends to demote objects in the sync block cache.
    virtual
    void SyncBlockCacheDemote(int max_gen) = 0;

    // Indicates to the EE that the GC has granted promotion to objects in the sync block cache.
    virtual
    void SyncBlockCachePromotionsGranted(int max_gen) = 0;

    // Queries whether or not the given thread has preemptive GC disabled.
    virtual
    bool IsPreemptiveGCDisabled(Thread * pThread) = 0;

    // Enables preemptive GC on the given thread.
    virtual
    void EnablePreemptiveGC(Thread * pThread) = 0;

    // Disables preemptive GC on the given thread.
    virtual
    void DisablePreemptiveGC(Thread * pThread) = 0;

    // Gets the Thread instance for the current thread, or null if no thread
    // instance is associated with this thread.
    //
    // If the GC created the current thread, GetThread returns null for threads
    // that were not created as suspendable (see `IGCHeap::CreateThread`).
    virtual
    Thread* GetThread() = 0;

    // Returns whether or not a thread suspension is pending.
    virtual
    bool TrapReturningThreads() = 0;

    // Retrieves the alloc context associated with a given thread.
    virtual
    gc_alloc_context * GetAllocContext(Thread * pThread) = 0;

    // Returns true if this thread is waiting to reach a safe point.
    virtual
    bool CatchAtSafePoint(Thread * pThread) = 0;

    // Calls the given enum_alloc_context_func with every active alloc context.
    virtual
    void GcEnumAllocContexts(enum_alloc_context_func* fn, void* param) = 0;

    // Creates and returns a new thread.
    // Parameters:
    //  threadStart - The function that will serve as the thread stub for the
    //                new thread. It will be invoked immediately upon the
    //                new thread upon creation.
    //  arg - The argument that will be passed verbatim to threadStart.
    //  is_suspendable - Whether or not the thread that is created should be suspendable
    //                   from a runtime perspective. Threads that are suspendable have
    //                   a VM Thread object associated with them that can be accessed
    //                   using `IGCHeap::GetThread`.
    //  name - The name of this thread, optionally used for diagnostic purposes.
    // Returns:
    //  true if the thread was started successfully, false if not.
    virtual
    bool CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name) = 0;

    // When a GC starts, gives the diagnostics code a chance to run.
    virtual
    void DiagGCStart(int gen, bool isInduced) = 0;

    // When GC heap segments change, gives the diagnostics code a chance to run.
    virtual
    void DiagUpdateGenerationBounds() = 0;

    // When a GC ends, gives the diagnostics code a chance to run.
    virtual
    void DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent) = 0;

    // During a GC after we discover what objects' finalizers should run, gives the diagnostics code a chance to run.
    virtual
    void DiagWalkFReachableObjects(void* gcContext) = 0;

    // During a GC after we discover the survivors and the relocation info, 
    // gives the diagnostics code a chance to run. This includes LOH if we are 
    // compacting LOH.
    virtual
    void DiagWalkSurvivors(void* gcContext) = 0;

    // During a full GC after we discover what objects to survive on LOH,
    // gives the diagnostics code a chance to run.
    virtual
    void DiagWalkLOHSurvivors(void* gcContext) = 0;

    // At the end of a background GC, gives the diagnostics code a chance to run.
    virtual
    void DiagWalkBGCSurvivors(void* gcContext) = 0;

    // Informs the EE of changes to the location of the card table, potentially updating the write
    // barrier if it needs to be updated.
    virtual
    void StompWriteBarrier(WriteBarrierParameters* args) = 0;

    // Signals to the finalizer thread that there are objects ready to
    // be finalized.
    virtual
    void EnableFinalization(bool foundFinalizers) = 0;

    // Signals to the EE that the GC encountered a fatal error and can't recover.
    virtual
    void HandleFatalError(unsigned int exitCode) = 0;

    // Asks the EE if it wants a particular object to be finalized when unloading
    // an app domain.
    virtual
    bool ShouldFinalizeObjectForUnload(AppDomain* pDomain, Object* obj) = 0;

    // Offers the EE the option to finalize the given object eagerly, i.e.
    // not on the finalizer thread but on the current thread. The
    // EE returns true if it finalized the object eagerly and the GC does not
    // need to do so, and false if it chose not to eagerly finalize the object
    // and it's up to the GC to finalize it later.
    virtual
    bool EagerFinalized(Object* obj) = 0;

    // Asks the EE if it wishes for the current GC to be a blocking GC. The GC will
    // only invoke this callback when it intends to do a full GC, so at this point
    // the EE can opt to elevate that collection to be a blocking GC and not a background one.
    virtual
    bool ForceFullGCToBeBlocking() = 0;

    // Retrieves the method table for the free object, a special kind of object used by the GC
    // to keep the heap traversable. Conceptually, the free object is similar to a managed array
    // of bytes: it consists of an object header (like all objects) and a "numComponents" field,
    // followed by some number of bytes of space that's free on the heap.
    //
    // The free object allows the GC to traverse the heap because it can inspect the numComponents
    // field to see how many bytes to skip before the next object on a heap segment begins.
    virtual
    MethodTable* GetFreeObjectMethodTable() = 0;

    // Asks the EE for the value of a given configuration key. If the EE does not know or does not
    // have a value for the requeested config key, false is returned and the value of the passed-in
    // pointer is undefined. Otherwise, true is returned and the config key's value is written to
    // the passed-in pointer.
    virtual
    bool GetBooleanConfigValue(const char* key, bool* value) = 0;

    virtual
    bool GetIntConfigValue(const char* key, int64_t* value) = 0;

    virtual
    bool GetStringConfigValue(const char* key, const char** value) = 0;

    virtual
    void FreeStringConfigValue(const char* value) = 0;

    // Returns true if this thread is a "GC thread", or a thread capable of
    // doing GC work. Threads are either /always/ GC threads
    // (if they were created for this purpose - background GC threads
    // and server GC threads) or they became GC threads by suspending the EE
    // and initiating a collection.
    virtual
    bool IsGCThread() = 0;

    // Returns true if the current thread is either a background GC thread
    // or a server GC thread.
    virtual
    bool WasCurrentThreadCreatedByGC() = 0;

    // Given an object, if this object is an instance of `System.Threading.OverlappedData`,
    // and the runtime treats instances of this class specially, traverses the objects that
    // are directly or (once) indirectly pinned by this object and reports them to the GC for
    // the purposes of relocation and promotion.
    //
    // Overlapped objects are very special and as such the objects they wrap can't be promoted in
    // the same manner as normal objects. This callback gives the EE the opportunity to hide these
    // details, if they are implemented at all.
    //
    // This function is a no-op if "object" is not an OverlappedData object.
    virtual
    void WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback) = 0;

    // Given an object, if this object is an instance of `System.Threading.OverlappedData` and the
    // runtime treats instances of this class specially, traverses the objects that are directly
    // or once indirectly pinned by this object and invokes the given callback on them. The callback
    // is passed the following arguments:
    //     Object* "from" - The object that "caused" the "to" object to be pinned. If a single object
    //                      is pinned directly by this OverlappedData, this object will be the
    //                      OverlappedData object itself. If an array is pinned by this OverlappedData,
    //                      this object will be the pinned array.
    //     Object* "to"   - The object that is pinned by the "from" object. If a single object is pinned
    //                      by an OverlappedData, "to" will be that single object. If an array is pinned
    //                      by an OverlappedData, the callback will be invoked on all elements of that
    //                      array and each element will be a "to" object.
    //     void* "context" - Passed verbatim from "WalkOverlappedObject" to the callback function.
    // The "context" argument will be passed directly to the callback without modification or inspection.
    //
    // This function is a no-op if "object" is not an OverlappedData object.
    virtual
    void WalkAsyncPinned(Object* object, void* context, void(*callback)(Object*, Object*, void*)) = 0;
};

#endif // _GCINTERFACE_EE_H_
