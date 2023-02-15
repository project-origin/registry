use core::slice;

use curve25519_dalek_ng::{ristretto::RistrettoPoint, scalar::Scalar};
use merlin::Transcript;

use crate::deref;


#[no_mangle]
pub unsafe extern "C" fn transcript_new(label: *const u8, len: usize) -> *const Transcript {
    let label = slice::from_raw_parts(label, len);
    Box::into_raw(Box::new(Transcript::new(label)))
}


#[no_mangle]
pub unsafe extern "C" fn transcript_append_point(
    this: *mut Transcript,
    label: *const u8,
    len: usize,
    point: *const RistrettoPoint
) {
    let label = slice::from_raw_parts(label, len);
    let point = deref!(point);
    (*this).append_message(label, point.compress().as_bytes());
}


#[no_mangle]
pub unsafe extern "C" fn transcript_domain(
    this: *mut Transcript,
    message: *const u8,
    len: usize,
) {
    let message = slice::from_raw_parts(message, len);
    (*this).append_message(b"domain-sep", message)
}



#[no_mangle]
pub unsafe extern "C" fn transcript_challenge_scalar(
    this: *mut Transcript,
    label: *const u8,
    len: usize,
) -> *const Scalar {
    let mut buf = [0u8; 64];
    let label = slice::from_raw_parts(label, len);
    (*this).challenge_bytes(label, &mut buf);

    let scalar = Scalar::from_bytes_mod_order_wide(&buf);
    Box::into_raw(Box::new(scalar))
}
