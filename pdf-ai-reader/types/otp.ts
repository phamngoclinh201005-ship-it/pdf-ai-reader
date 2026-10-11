export interface OtpTiming { expiresAt: string; resendAvailableAt: string; serverTime: string }
export interface OtpDelivery { message: string; timing: OtpTiming }
export interface OtpChallenge { email: string; expiresAt: number; resendAt: number }
