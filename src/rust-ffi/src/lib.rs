#![allow(clippy::missing_safety_doc)]
#![allow(clippy::not_unsafe_ptr_arg_deref)]

use core::slice;

use util::RawVec;
pub mod generator;
pub mod point;
pub mod rangeproof;
pub mod scalar;
mod util;
pub mod transscript;

#[no_mangle]
pub unsafe extern "C" fn fill_bytes(raw: *const RawVec<u8>, dst: *mut u8) {
    let raw = deref!(raw);
    let src = Vec::from_raw_parts(raw.data, raw.size, raw.cap);
    let dst = slice::from_raw_parts_mut(dst, raw.size);
    dst.clone_from_slice(&src);
}

#[no_mangle]
pub unsafe extern "C" fn free_vec(raw: RawVec<u8>) {
    let vec = Vec::from_raw_parts(raw.data, raw.size, raw.cap); // dies here
    drop(vec);
}
