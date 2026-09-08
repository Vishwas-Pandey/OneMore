// ATTBridge.mm
// Native bridge to Apple's AppTrackingTransparency framework, called from
// ATTManager.cs. Requires Info.plist key NSUserTrackingUsageDescription
// (added automatically by Assets/Editor/IOSBuildPostProcessor.cs) and the
// AppTrackingTransparency + AdSupport frameworks (also linked by that
// post-process script).
#import <AppTrackingTransparency/ATTrackingManager.h>
#import <Foundation/Foundation.h>

extern "C" {

    // Blocks the calling thread until the user responds to the system dialog
    // (or resolves immediately if status is already determined). Called from
    // C# on a background-safe path; Unity's DllImport call itself is synchronous
    // from the C# side, so we use a semaphore to wait for Apple's async callback.
    int _ATTRequestAuthorization(void) {
        if (@available(iOS 14, *)) {
            __block ATTrackingManagerAuthorizationStatus resultStatus = ATTrackingManagerAuthorizationStatusNotDetermined;
            dispatch_semaphore_t sema = dispatch_semaphore_create(0);

            dispatch_async(dispatch_get_main_queue(), ^{
                [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                    resultStatus = status;
                    dispatch_semaphore_signal(sema);
                }];
            });

            // Wait up to 60s for the user to dismiss the system dialog.
            dispatch_semaphore_wait(sema, dispatch_time(DISPATCH_TIME_NOW, 60LL * NSEC_PER_SEC));
            return (int)resultStatus;
        }
        return 3; // Authorized on iOS < 14, no gate exists.
    }

    int _ATTGetStatus(void) {
        if (@available(iOS 14, *)) {
            return (int)[ATTrackingManager trackingAuthorizationStatus];
        }
        return 3;
    }

}
