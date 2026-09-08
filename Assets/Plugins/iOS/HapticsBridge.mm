// HapticsBridge.mm
// Native iOS haptics bridge for Unity. Unity's C# layer has no wrapper around
// UIFeedbackGenerator, so HapticManager.cs calls these C functions via
// [DllImport("__Internal")]. Unity automatically compiles any .mm file placed
// under Assets/Plugins/iOS into the generated Xcode project.
#import <UIKit/UIKit.h>

// Must match HapticManager.HapticStyle: 0 = Light, 1 = Medium, 2 = Heavy
extern "C" {

    void _HapticImpact(int style) {
        if (@available(iOS 10.0, *)) {
            UIImpactFeedbackStyle feedbackStyle;
            switch (style) {
                case 0: feedbackStyle = UIImpactFeedbackStyleLight; break;
                case 1: feedbackStyle = UIImpactFeedbackStyleMedium; break;
                default: feedbackStyle = UIImpactFeedbackStyleHeavy; break;
            }

            dispatch_async(dispatch_get_main_queue(), ^{
                UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:feedbackStyle];
                [generator prepare];
                [generator impactOccurred];
            });
        }
    }

    bool _HapticsAvailable(void) {
        return true;
    }

}
