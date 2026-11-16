// Plays game audio even when the iPhone's ring/silent switch is on.
// Unity's default session category is Ambient, which the silent switch mutes, so on most
// players' phones the game was completely silent. Playback ignores the switch; MixWithOthers
// keeps the player's own music or podcast running underneath instead of stopping it.
#import <AVFoundation/AVFoundation.h>

extern "C" void DrawRush_UsePlaybackAudioSession()
{
    AVAudioSession *session = [AVAudioSession sharedInstance];
    NSError *error = nil;
    if (![session setCategory:AVAudioSessionCategoryPlayback
                  withOptions:AVAudioSessionCategoryOptionMixWithOthers
                        error:&error])
    {
        NSLog(@"[DrawRushAudio] setCategory failed: %@", error);
        return;
    }
    if (![session setActive:YES error:&error])
        NSLog(@"[DrawRushAudio] setActive failed: %@", error);
}
