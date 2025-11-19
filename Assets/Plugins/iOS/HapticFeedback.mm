#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>

extern "C" {
    
    // Continuous haptic feedback state
    static NSTimer *continuousHapticTimer = nil;
    static UISelectionFeedbackGenerator *continuousGenerator = nil;
    
    // Light impact haptic feedback
    void _TriggerImpactLight() {
        if (@available(iOS 10.0, *)) {
            UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            [generator prepare];
            [generator impactOccurred];
        }
    }
    
    // Medium impact haptic feedback
    void _TriggerImpactMedium() {
        if (@available(iOS 10.0, *)) {
            UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            [generator prepare];
            [generator impactOccurred];
        }
    }
    
    // Heavy impact haptic feedback
    void _TriggerImpactHeavy() {
        if (@available(iOS 10.0, *)) {
            UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            [generator prepare];
            [generator impactOccurred];
        }
    }
    
    // Selection haptic feedback (for UI interactions)
    void _TriggerSelection() {
        if (@available(iOS 10.0, *)) {
            UISelectionFeedbackGenerator *generator = [[UISelectionFeedbackGenerator alloc] init];
            [generator prepare];
            [generator selectionChanged];
        }
    }
    
    // Notification success haptic feedback
    void _TriggerNotificationSuccess() {
        if (@available(iOS 10.0, *)) {
            UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
            [generator prepare];
            [generator notificationOccurred:UINotificationFeedbackTypeSuccess];
        }
    }
    
    // Notification warning haptic feedback
    void _TriggerNotificationWarning() {
        if (@available(iOS 10.0, *)) {
            UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
            [generator prepare];
            [generator notificationOccurred:UINotificationFeedbackTypeWarning];
        }
    }
    
    // Notification error haptic feedback
    void _TriggerNotificationError() {
        if (@available(iOS 10.0, *)) {
            UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
            [generator prepare];
            [generator notificationOccurred:UINotificationFeedbackTypeError];
        }
    }
    
    // Start continuous haptic feedback (for dragging)
    void _StartContinuousHaptic() {
        if (@available(iOS 10.0, *)) {
            // Stop any existing continuous haptic
            _StopContinuousHaptic();
            
            // Create a new selection generator for continuous feedback
            continuousGenerator = [[UISelectionFeedbackGenerator alloc] init];
            [continuousGenerator prepare];
            
            // Trigger initial haptic
            [continuousGenerator selectionChanged];
            
            // Create a timer to trigger haptic feedback every 100ms (10 times per second)
            continuousHapticTimer = [NSTimer scheduledTimerWithTimeInterval:0.1
                                                                     repeats:YES
                                                                       block:^(NSTimer * _Nonnull timer) {
                if (continuousGenerator != nil) {
                    [continuousGenerator selectionChanged];
                    [continuousGenerator prepare];
                }
            }];
        }
    }
    
    // Stop continuous haptic feedback
    void _StopContinuousHaptic() {
        if (continuousHapticTimer != nil) {
            [continuousHapticTimer invalidate];
            continuousHapticTimer = nil;
        }
        
        if (continuousGenerator != nil) {
            continuousGenerator = nil;
        }
    }
}
