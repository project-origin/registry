#![allow(clippy::missing_safety_doc)]
#![allow(clippy::not_unsafe_ptr_arg_deref)]

use core::slice;

use util::RawVec;
pub mod generator;
pub mod point;
pub mod rangeproof;
pub mod scalar;
mod util;

#[no_mangle]
pub unsafe extern "C" fn fill_bytes(raw: *const RawVec, dst: *mut u8) {
    let raw = deref!(raw);
    let src = Vec::from_raw_parts(raw.ptr, raw.size, raw.cap);
    let dst = slice::from_raw_parts_mut(dst, raw.size);
    dst.clone_from_slice(&src);
}

#[no_mangle]
pub unsafe extern "C" fn free_vec(raw: *const RawVec) {
    let raw = deref!(raw);
    // dbg!(&raw);
    let vec = Vec::from_raw_parts(raw.ptr, raw.size, raw.cap);
    drop(vec);
}
